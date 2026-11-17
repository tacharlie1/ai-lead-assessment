namespace PrReviewAssistant.Changes;

/// <summary>
/// Turns the text of a unified diff (the output of <c>git diff</c>) into a <see cref="ChangeSet"/>.
/// It reads file paths, the kind of change, and the lines of each hunk.
/// </summary>
public static class DiffParser
{
    public static ChangeSet Parse(string diffText, string repository, string branch)
    {
        var files = new List<FileChange>();

        // State for the file and hunk currently being read.
        string? path = null;
        var kind = ChangeKind.Modified;
        var hunks = new List<Hunk>();
        string? oldPath = null;
        var isBinary = false;
        string? hunkHeader = null;
        var lines = new List<DiffLine>();

        void FinishHunk()
        {
            if (hunkHeader is not null)
            {
                hunks.Add(new Hunk(hunkHeader, lines));
            }

            hunkHeader = null;
            lines = [];
        }

        void FinishFile()
        {
            FinishHunk();
            if (path is not null)
            {
                files.Add(new FileChange(path, kind, hunks, oldPath, isBinary));
            }

            path = null;
            kind = ChangeKind.Modified;
            hunks = [];
            oldPath = null;
            isBinary = false;
        }

        foreach (var line in diffText.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.StartsWith("diff --git "))
            {
                // A new file starts, e.g. "diff --git a/src/MathService.cs b/src/MathService.cs".
                FinishFile();
                path = ReadPathFromDiffHeader(line);
            }
            else if (line.StartsWith("new file mode"))
            {
                kind = ChangeKind.Added;
            }
            else if (line.StartsWith("deleted file mode"))
            {
                kind = ChangeKind.Deleted;
            }
            else if (line.StartsWith("rename from "))
            {
                // e.g. "rename from src/MathService.cs". The new name is already in the "diff --git" line.
                kind = ChangeKind.Renamed;
                oldPath = line["rename from ".Length..];
            }
            else if (line.StartsWith("Binary files ") || line == "GIT binary patch")
            {
                // e.g. "Binary files /dev/null and b/logo.png differ". There are no lines to read.
                isBinary = true;
            }
            else if (line.StartsWith("@@"))
            {
                // A new hunk starts, e.g. "@@ -8,6 +8,11 @@ public class MathService".
                FinishHunk();
                hunkHeader = ReadHunkHeader(line);
            }
            else if (hunkHeader is null)
            {
                // File header lines such as "index ...", "--- a/..." and "+++ b/..." carry nothing we need.
            }
            else if (line.StartsWith('+'))
            {
                lines.Add(new DiffLine(DiffLineKind.Added, line[1..]));
            }
            else if (line.StartsWith('-'))
            {
                lines.Add(new DiffLine(DiffLineKind.Removed, line[1..]));
            }
            else if (line.StartsWith(' ') || line.Length == 0)
            {
                // Unchanged context. Some tools strip the leading space from blank lines.
                lines.Add(new DiffLine(DiffLineKind.Context, line.Length > 0 ? line[1..] : ""));
            }
        }

        FinishFile();
        return new ChangeSet(repository, branch, files);
    }

    private static string ReadPathFromDiffHeader(string line)
    {
        // Take the "b/..." path, which is the file's name after the change.
        var bIndex = line.LastIndexOf(" b/", StringComparison.Ordinal);
        return bIndex >= 0 ? line[(bIndex + 3)..] : line["diff --git ".Length..];
    }

    private static string ReadHunkHeader(string line)
    {
        // Everything after the second "@@" is the text git found above this hunk.
        var end = line.IndexOf("@@", 2, StringComparison.Ordinal);
        return end >= 0 ? line[(end + 2)..].Trim() : string.Empty;
    }
}
