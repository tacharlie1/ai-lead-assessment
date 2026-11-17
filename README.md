# PR Review Assistant (local pilot)

A small .NET 8 command-line tool that reads a code change (a `git diff`) and writes a review as a Markdown file: what changed and why it matters, potential risks, suggested tests and other suggestions.

It runs entirely on your machine. It does not call GitHub or any network service. Review generation sits behind a swappable provider interface; this pilot ships a **simulated** rule-based provider, and [SOLUTION.md](SOLUTION.md) explains how a Claude adapter would plug in.

This repository also contains the AI adoption plan: [PLAN.md](PLAN.md).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Check with `dotnet --version` (should print `8.x`).

## Run the sample

From the repository root:

```bash
dotnet run --project src/PrReviewAssistant -- \
  --diff samples/feature-minor-fix.diff \
  --repo Sample.DotNetApp \
  --branch feature/minor-fix \
  --out review.md
```

Open `review.md`. A second sample shows the risk checks:

```bash
dotnet run --project src/PrReviewAssistant -- \
  --diff samples/risky-change.diff \
  --repo Sample.DotNetApp \
  --branch feature/order-confirmation \
  --out review.md
```

Pre-generated output for both samples is in [`samples/`](samples/) if you want to see the result without running anything.

## Review your own changes

In any git repository:

```bash
git diff main > changes.diff
dotnet run --project /path/to/this/repo/src/PrReviewAssistant -- --diff changes.diff --repo MyApp --branch my-branch
```

For better method names in the review, tell git to read C# structure: add `*.cs diff=csharp` to the repository's `.gitattributes`.

## Options

| Option | Meaning | Default |
|---|---|---|
| `--diff <file>` | Unified diff to review. Required. | |
| `--repo <name>` | Repository name shown in the report. | `local-repo` |
| `--branch <name>` | Branch name shown in the report. | `working-changes` |
| `--out <file>` | Where to write the Markdown review. | `review.md` |
| `--provider <name>` | Review provider to use. | `simulated` |
| `--help` | Show help. | |

## Run the tests

```bash
dotnet test
```

## Project layout

```
src/PrReviewAssistant/
  Program.cs                      Entry point: read diff -> review -> write Markdown
  CommandLineOptions.cs           Command-line parsing
  Changes/                        ChangeSet model and the diff parser
  Review/                         IReviewProvider (the extension point), ReviewResult, provider factory
  Providers/                      SimulatedReviewProvider (rule-based, offline)
  Output/                         MarkdownReportWriter
tests/PrReviewAssistant.Tests/    xUnit tests
samples/                          Sample diffs and the reviews generated from them
```
