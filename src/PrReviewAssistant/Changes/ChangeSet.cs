namespace PrReviewAssistant.Changes;

/// <summary>
/// Everything the reviewer needs to know about a change: where it comes from and which files changed.
/// </summary>
public sealed record ChangeSet(string Repository, string Branch, IReadOnlyList<FileChange> Files)
{
    public int TotalAdded => Files.Sum(f => f.AddedLines.Count);
    public int TotalRemoved => Files.Sum(f => f.RemovedLines.Count);
}

/// <summary>One changed file and the hunks (blocks of changed lines) inside it.</summary>
public sealed record FileChange(string Path, ChangeKind Kind, IReadOnlyList<Hunk> Hunks)
{
    public IReadOnlyList<string> AddedLines => Hunks.SelectMany(h => h.AddedLines).ToList();
    public IReadOnlyList<string> RemovedLines => Hunks.SelectMany(h => h.RemovedLines).ToList();

    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>True for files that look like unit tests, e.g. "MathServiceTests.cs".</summary>
    public bool IsTestFile => Path.Contains("Test", StringComparison.OrdinalIgnoreCase);

    public bool IsCSharp => Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// A block of changes. <paramref name="Header"/> is the text git prints after the "@@" marker:
/// the nearest line above the hunk that looks like a method or class.
/// </summary>
public sealed record Hunk(string Header, IReadOnlyList<DiffLine> Lines)
{
    public IReadOnlyList<string> AddedLines => TextOf(DiffLineKind.Added);
    public IReadOnlyList<string> RemovedLines => TextOf(DiffLineKind.Removed);

    private List<string> TextOf(DiffLineKind kind) =>
        Lines.Where(line => line.Kind == kind).Select(line => line.Text).ToList();
}

/// <summary>One line of a hunk: added ("+"), removed ("-") or unchanged context (" ").</summary>
public sealed record DiffLine(DiffLineKind Kind, string Text);

public enum DiffLineKind
{
    Context,
    Added,
    Removed,
}

public enum ChangeKind
{
    Added,
    Modified,
    Deleted,
}
