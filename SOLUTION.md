# Solution: Local PR Review Assistant

## What it does

The tool reads a unified diff, produces a review, and writes it to a local Markdown file. The review covers what changed and why it matters, potential risks, suggested tests and other suggestions, plus a table of changed files. Nothing is posted anywhere and no network access is needed.

## Design

The program is a straight pipeline of four small parts:

```
diff file ──► DiffParser ──► ChangeSet ──► IReviewProvider ──► ReviewResult ──► MarkdownReportWriter ──► review.md
                                          (simulated today,
                                           Claude later)
```

| Part | File | Job |
|---|---|---|
| Diff parser | `Changes/DiffParser.cs` | Turns `git diff` text into a `ChangeSet`: files, change kind, and hunks with added, removed and context lines. |
| Change model | `Changes/ChangeSet.cs` | Plain records. No logic beyond simple counts. |
| **Extension point** | `Review/IReviewProvider.cs` | One method: `ReviewAsync(ChangeSet) → ReviewResult`. Every provider implements it. |
| Provider factory | `Review/ReviewProviderFactory.cs` | Maps a name (`--provider simulated`) to a provider. Adding a provider is one line here. |
| Simulated provider | `Providers/SimulatedReviewProvider.cs` | Rule-based reviewer, offline and deterministic. |
| Report writer | `Output/MarkdownReportWriter.cs` | Renders the result as Markdown. Does not know which provider was used, apart from printing its name. |

The parser and the report writer never depend on a specific provider, so swapping the provider changes nothing else.

### The simulated provider

Each rule is one short method, so it is easy to see why a comment appears. The rules cover:

- **Summary:** each file's change and size, new guard clauses (`throw new ...`) with the method they sit in, and the number of new unit tests.
- **Risks:**
  - behaviour changes from new guards;
  - changed or removed public signatures;
  - `async void`, blocking on tasks (`.Result`, `.Wait()`), catch-all and empty catch blocks;
  - hard-coded secrets (the value is masked in the report);
  - deleted files and configuration changes.
- **Tests:** code changes with no matching test file, and boundary tests for new guards.
- **Suggestions:** consistent exception conventions, TODO notes, and very large changes.

Git's hunk header names the nearest line *above* the hunk, which is often the wrong method. The provider looks back through the hunk's own lines for the real method signature first, so a guard inside `Divide` is reported as `MathService.Divide`, not `Add`.

## Trade-offs

- **Diff text as input, not whole files.** A diff is what a team already has (`git diff`) and is small. The cost is limited context: the reviewer sees changed lines and a few lines around them, not the whole class. A Claude adapter would benefit from more context, at the cost of sending more code.
- **Rules, not intelligence.** The simulated provider is predictable and testable, and it is honest about what it is. It can't judge intent or design, and it will miss problems no rule describes. That is the gap a real model fills.
- **Simple heuristics.** "Test file" means the path contains `Test`, and "has tests" means a test file name contains the class name. These are good enough for a pilot, but will be wrong in some repositories.
- **Hand-written argument parsing** instead of a library. This keeps dependencies at zero, at the cost of fewer features (no short flags, no combined options).
- **Advisory output only.** The report says so, and nothing is blocked automatically. That's deliberate for a pilot: trust has to be earned before a tool gates merges.

## Plugging in a Claude / Claude Code adapter

The adapter is one new class that implements `IReviewProvider`, plus one line in the factory:

```csharp
["simulated"] = () => new SimulatedReviewProvider(),
["claude"]    = () => new ClaudeReviewProvider(ClaudeSettings.FromEnvironment()),
```

Two ways to build `ClaudeReviewProvider`:

1. **Claude API (preferred for this tool).** Build a prompt from the `ChangeSet` (instructions, then the diff clearly marked as data) and call the Messages API through `HttpClient` or the official SDK. Ask for JSON with `summary`, `risks`, `suggestedTests` and `suggestions`, and map it to `ReviewResult`. If the JSON is invalid, report a clear error rather than writing a half-finished review. The model gets text in and text out, with no tools.
2. **Claude Code in headless mode.** Run `claude -p "<prompt>" --output-format json` as a child process, with read-only tools (for example `--allowedTools "Read,Grep,Glob"`) and no shell, edit or web tools. This lets Claude read more of the repository for context. See [Claude Code headless mode](https://code.claude.com/docs/en/headless).

Because the report writer only sees a `ReviewResult`, the output format stays the same for every provider.

### Enabling it safely under strict security

- **Off by default.** The simulated provider stays the default. Claude runs only when someone passes `--provider claude` and the adapter is configured.
- **Approved route only.** The endpoint comes from configuration, so it can point at the route security approved: an enterprise account with agreed data retention, or a model hosted in our existing cloud agreement. No other endpoint is hard-coded.
- **Credentials outside the code.** The key comes from an environment variable or the company vault, never from the repository or a config file. It's never logged.
- **Send as little as possible.**
  - Only the diff is sent, with a size limit.
  - Files on a deny list (`.env`, `appsettings.*.json`, certificates, customer data folders) are dropped before sending.
  - Secret-looking values are masked with the same check the simulated provider uses.
- **Treat the diff as untrusted data.** The prompt marks it as content to review, not instructions to follow. The API adapter has no tools, and the Claude Code adapter is read-only with no network or shell, so a prompt hidden in the code can't make it act.
- **Local audit log.** Each run records which files were sent, their sizes and a hash, the provider and the time, but not the code itself. Security can see what left the machine.
- **Human stays in charge.** The output is still a local, advisory Markdown file. The tool never comments on, approves or blocks a PR.
- **Staged rollout.** Pilot repositories first, under the approval and usage standards in [PLAN.md](PLAN.md), with the simulated provider as the fallback.
