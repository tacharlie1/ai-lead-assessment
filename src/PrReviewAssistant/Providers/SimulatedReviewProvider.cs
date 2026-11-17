using System.Text.RegularExpressions;
using PrReviewAssistant.Changes;
using PrReviewAssistant.Review;

namespace PrReviewAssistant.Providers;

/// <summary>
/// A stand-in for an AI model. It reviews a change set with simple, predictable rules, works offline
/// and gives the same output every time. Each rule is one small method below, so a reader can see
/// exactly why a comment appears in the review.
/// </summary>
public sealed class SimulatedReviewProvider : IReviewProvider
{
    private const int LargeChangeLineCount = 300;
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    private static readonly Regex ThrowNew = new(@"throw\s+new\s+(\w+)", Options);
    private static readonly Regex MethodSignature = new(@"\b(?:public|private|protected|internal)\b[^=;{]*?(\w+)\s*\(", Options);
    private static readonly Regex PublicMember = new(@"^\s*public\s+[^=;]*\(", Options);
    private static readonly Regex TestAttribute = new(@"\[(Fact|Theory|Test|TestMethod)\b", Options);
    private static readonly Regex Todo = new(@"//\s*(TODO|FIXME|HACK)\b", Options | RegexOptions.IgnoreCase);

    /// <summary>Code patterns that are often a problem in C#, with the reason to look at them.</summary>
    private static readonly (Regex Pattern, string Reason)[] RiskyCode =
    [
        (new(@"\basync\s+void\b", Options),
            "`async void` means callers cannot await it or catch its exceptions. Return `Task` instead."),
        (new(@"\.Result\b|\.Wait\(\)", Options),
            "blocking on a task can deadlock and ties up threads. Use `await` instead."),
        (new(@"catch\s*\(\s*Exception\b", Options),
            "catching every exception can hide real failures. Catch the specific exceptions you expect."),
        (new(@"catch\s*(\([^)]*\))?\s*\{\s*\}", Options),
            "an empty catch block silently swallows errors."),
    ];

    private static readonly Regex HardCodedSecret =
        new(@"(password|secret|apikey|api_key|token)\w*\s*=\s*""[^""]+""", Options | RegexOptions.IgnoreCase);

    private static readonly Regex StringLiteral = new(@"""[^""]*""", Options);

    private static readonly string[] ConfigFileEndings = [".csproj", ".config", ".json", ".props", ".yml", ".yaml"];

    public string Name => "simulated";

    public Task<ReviewResult> ReviewAsync(ChangeSet changes, CancellationToken cancellationToken = default)
    {
        var notes = new ReviewNotes();

        if (changes.Files.Count == 0)
        {
            notes.Summary.Add("The diff contains no file changes.");
            return Task.FromResult(notes.ToResult());
        }

        // Lines added to test files anywhere in the change, so rules can see what is already tested.
        var newTestLines = changes.Files.Where(f => f.IsTestFile).SelectMany(f => f.AddedLines).ToList();

        foreach (var file in changes.Files)
        {
            DescribeFile(file, notes);
            CheckDeletedFile(file, notes);
            CheckRenamedFile(file, notes);
            CheckConfigurationChange(file, notes);

            if (file.IsCSharp)
            {
                CheckNewTests(file, notes);
                CheckGuardClauses(file, newTestLines, notes);
                CheckPublicSignatureChanges(file, notes);
                CheckRiskyCode(file, notes);
                CheckHardCodedSecrets(file, notes);
                CheckTodos(file, notes);
            }
        }

        CheckMissingTests(changes, notes);
        CheckChangeSize(changes, notes);

        return Task.FromResult(notes.ToResult());
    }

    // ---- Rules -------------------------------------------------------------------------------

    private static void DescribeFile(FileChange file, ReviewNotes notes)
    {
        var lineCounts = $"(+{file.AddedLines.Count} / -{file.RemovedLines.Count} lines)";

        var description = (file.Kind, file.IsBinary) switch
        {
            (ChangeKind.Added, true) => $"Adds binary file `{file.Path}`.",
            (ChangeKind.Deleted, true) => $"Deletes binary file `{file.Path}`.",
            (ChangeKind.Renamed, true) => $"Renames binary file `{file.OldPath}` to `{file.Path}`.",
            (_, true) => $"Modifies binary file `{file.Path}`.",
            (ChangeKind.Added, _) => $"Adds `{file.Path}` (+{file.AddedLines.Count} lines).",
            (ChangeKind.Deleted, _) => $"Deletes `{file.Path}` (-{file.RemovedLines.Count} lines).",
            (ChangeKind.Renamed, _) when file.IsPureRename => $"Renames `{file.OldPath}` to `{file.Path}` (content unchanged).",
            (ChangeKind.Renamed, _) => $"Renames `{file.OldPath}` to `{file.Path}` and changes it {lineCounts}.",
            _ => $"Modifies `{file.Path}` {lineCounts}.",
        };
        notes.Summary.Add(description);
    }

    private static void CheckRenamedFile(FileChange file, ReviewNotes notes)
    {
        if (file.Kind == ChangeKind.Renamed && file.IsCSharp)
        {
            notes.Risks.Add($"`{file.OldPath}` is renamed to `{file.FileName}`. " +
                            "Check the class name, namespace and any references still match.");
        }
    }

    private static void CheckDeletedFile(FileChange file, ReviewNotes notes)
    {
        if (file.Kind == ChangeKind.Deleted)
        {
            notes.Risks.Add($"`{file.Path}` is deleted. Check nothing else still references it.");
        }
    }

    private static void CheckConfigurationChange(FileChange file, ReviewNotes notes)
    {
        if (ConfigFileEndings.Any(ending => file.Path.EndsWith(ending, StringComparison.OrdinalIgnoreCase)))
        {
            notes.Risks.Add($"`{file.Path}` is a project or configuration file. Check the change is right for every environment.");
        }
    }

    private static void CheckNewTests(FileChange file, ReviewNotes notes)
    {
        var newTests = file.AddedLines.Count(line => TestAttribute.IsMatch(line));
        if (file.IsTestFile && newTests > 0)
        {
            notes.Summary.Add($"Adds {newTests} unit {Plural(newTests, "test")} in `{file.FileName}`.");
        }
    }

    /// <summary>A new <c>throw new ...</c> usually means a new guard: the behaviour changes for some inputs.</summary>
    private static void CheckGuardClauses(FileChange file, IReadOnlyList<string> newTestLines, ReviewNotes notes)
    {
        if (file.IsTestFile)
        {
            return;
        }

        foreach (var hunk in file.Hunks)
        {
            for (var index = 0; index < hunk.Lines.Count; index++)
            {
                var line = hunk.Lines[index];
                var match = ThrowNew.Match(line.Text);
                if (line.Kind != DiffLineKind.Added || !match.Success)
                {
                    continue;
                }

                var exception = match.Groups[1].Value;
                var method = FindMethodName(hunk, index);
                var className = Path.GetFileNameWithoutExtension(file.Path);
                var location = method is null ? $"`{file.FileName}`" : $"`{className}.{method}`";

                notes.Summary.Add($"Adds a guard to {location} that throws `{exception}`.");
                notes.Risks.Add($"Behaviour change in {location}: input that used to return a result now throws `{exception}`. " +
                                "Existing callers may rely on the old behaviour, so communicate the change.");

                if (IsCoveredByNewTest(newTestLines, exception, method))
                {
                    notes.SuggestedTests.Add($"The guard in {location} is covered by a new test. " +
                                             "Consider boundary tests around it (for example negative values).");
                }
                else
                {
                    notes.SuggestedTests.Add($"Test that {location} throws `{exception}` for the guarded input, " +
                                             "and add boundary tests around it (for example negative values).");
                }

                notes.Suggestions.Add($"Check `{exception}` and its message match the convention used by similar methods.");
            }
        }
    }

    /// <summary>A removed public signature that was not added back unchanged may break callers.</summary>
    private static void CheckPublicSignatureChanges(FileChange file, ReviewNotes notes)
    {
        if (file.IsTestFile)
        {
            return;
        }

        var addedSignatures = file.AddedLines.Select(line => line.Trim()).ToHashSet();

        foreach (var removed in file.RemovedLines.Where(line => PublicMember.IsMatch(line)))
        {
            if (!addedSignatures.Contains(removed.Trim()))
            {
                notes.Risks.Add($"Public member changed or removed in `{file.FileName}`: `{Snippet(removed)}`. " +
                                "Callers may break. Check usages before merging.");
            }
        }
    }

    private static void CheckRiskyCode(FileChange file, ReviewNotes notes)
    {
        foreach (var (pattern, reason) in RiskyCode)
        {
            // Report each pattern once per file, at its first occurrence.
            var line = file.AddedLines.FirstOrDefault(pattern.IsMatch);
            if (line is not null)
            {
                notes.Risks.Add($"In `{file.FileName}`, `{Snippet(line)}`: {reason}");
            }
        }
    }

    /// <summary>Flags likely secrets. The value is masked so the review never repeats it.</summary>
    private static void CheckHardCodedSecrets(FileChange file, ReviewNotes notes)
    {
        foreach (var line in file.AddedLines.Where(line => HardCodedSecret.IsMatch(line)))
        {
            var masked = StringLiteral.Replace(line, "\"***\"");
            notes.Risks.Add($"In `{file.FileName}`, `{Snippet(masked)}`: this looks like a hard-coded secret. " +
                            "Move it to configuration or a secrets vault, and rotate it if it was ever committed.");
        }
    }

    private static void CheckTodos(FileChange file, ReviewNotes notes)
    {
        foreach (var line in file.AddedLines.Where(line => Todo.IsMatch(line)))
        {
            notes.Suggestions.Add($"Resolve or create a ticket for the note added in `{file.FileName}`: `{Snippet(line)}`.");
        }
    }

    /// <summary>Code files changed without a matching test file in the same change set.</summary>
    private static void CheckMissingTests(ChangeSet changes, ReviewNotes notes)
    {
        var testFiles = changes.Files.Where(f => f.IsTestFile).ToList();
        // Only files whose code changed need tests; deleted files and pure renames do not.
        var codeFiles = changes.Files.Where(f =>
            f.IsCSharp && !f.IsTestFile && f.Kind != ChangeKind.Deleted && !f.IsPureRename);

        foreach (var code in codeFiles)
        {
            var className = Path.GetFileNameWithoutExtension(code.Path);
            var hasTests = testFiles.Any(test => test.FileName.Contains(className, StringComparison.OrdinalIgnoreCase));

            if (!hasTests)
            {
                notes.SuggestedTests.Add($"No test changes accompany `{code.FileName}`. Add or update tests for the changed behaviour.");
            }
        }
    }

    private static void CheckChangeSize(ChangeSet changes, ReviewNotes notes)
    {
        var changedLines = changes.TotalAdded + changes.TotalRemoved;
        if (changedLines > LargeChangeLineCount)
        {
            notes.Suggestions.Add($"This change touches {changedLines} lines. Smaller PRs are faster and safer to review; consider splitting it.");
        }
    }

    // ---- Helpers -----------------------------------------------------------------------------

    /// <summary>
    /// Finds the method a line sits in, e.g. "Divide". It looks upwards through the hunk for a method
    /// signature, then falls back to git's hunk header. Returns null if neither names a method.
    /// </summary>
    private static string? FindMethodName(Hunk hunk, int lineIndex)
    {
        for (var i = lineIndex - 1; i >= 0; i--)
        {
            var signature = MethodSignature.Match(hunk.Lines[i].Text);
            if (signature.Success && hunk.Lines[i].Kind != DiffLineKind.Removed)
            {
                return signature.Groups[1].Value;
            }
        }

        var header = MethodSignature.Match(hunk.Header);
        return header.Success ? header.Groups[1].Value : null;
    }

    /// <summary>
    /// True when a new test in this change already mentions both the exception and the method,
    /// e.g. <c>Assert.Throws&lt;DivideByZeroException&gt;(() =&gt; _service.Divide(10, 0))</c>.
    /// </summary>
    private static bool IsCoveredByNewTest(IReadOnlyList<string> newTestLines, string exception, string? method) =>
        newTestLines.Any(line => line.Contains(exception) && (method is null || line.Contains(method)));

    private static string Snippet(string line)
    {
        const int maxLength = 70;
        var trimmed = line.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "...";
    }

    private static string Plural(int count, string word) => count == 1 ? word : word + "s";

    /// <summary>Collects comments while the rules run, then removes duplicates.</summary>
    private sealed class ReviewNotes
    {
        public List<string> Summary { get; } = [];
        public List<string> Risks { get; } = [];
        public List<string> SuggestedTests { get; } = [];
        public List<string> Suggestions { get; } = [];

        public ReviewResult ToResult() => new(
            Summary.Distinct().ToList(),
            Risks.Distinct().ToList(),
            SuggestedTests.Distinct().ToList(),
            Suggestions.Distinct().ToList());
    }
}
