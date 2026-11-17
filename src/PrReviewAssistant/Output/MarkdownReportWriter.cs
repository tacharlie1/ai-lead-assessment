using System.Text;
using PrReviewAssistant.Changes;
using PrReviewAssistant.Review;

namespace PrReviewAssistant.Output;

/// <summary>Turns a review into the Markdown file a human reviewer reads.</summary>
public static class MarkdownReportWriter
{
    public static string Render(ChangeSet changes, ReviewResult review, string providerName, DateTimeOffset generatedAt)
    {
        var md = new StringBuilder();

        md.AppendLine($"# AI Review: {changes.Repository} ({changes.Branch})");
        md.AppendLine();
        md.AppendLine($"_Provider: {providerName} · Generated: {generatedAt:yyyy-MM-dd HH:mm} · " +
                      $"{changes.Files.Count} {(changes.Files.Count == 1 ? "file" : "files")} changed " +
                      $"(+{changes.TotalAdded} / -{changes.TotalRemoved})_");
        md.AppendLine();
        md.AppendLine("> This review is advisory. A human reviewer makes the final decision.");
        md.AppendLine();

        AppendSection(md, "Summary", review.Summary);
        AppendSection(md, "Potential Risks", review.Risks);
        AppendSection(md, "Suggested Tests", review.SuggestedTests);
        AppendSection(md, "Suggestions", review.Suggestions);
        AppendFileTable(md, changes);

        return md.ToString();
    }

    private static void AppendSection(StringBuilder md, string title, IReadOnlyList<string> items)
    {
        md.AppendLine($"## {title}");
        md.AppendLine();

        if (items.Count == 0)
        {
            md.AppendLine("- None found.");
        }

        foreach (var item in items)
        {
            md.AppendLine($"- {item}");
        }

        md.AppendLine();
    }

    private static void AppendFileTable(StringBuilder md, ChangeSet changes)
    {
        md.AppendLine("## Files Changed");
        md.AppendLine();
        md.AppendLine("| File | Change | Added | Removed |");
        md.AppendLine("|---|---|---:|---:|");

        foreach (var file in changes.Files)
        {
            md.AppendLine($"| `{file.Path}` | {file.Kind} | {file.AddedLines.Count} | {file.RemovedLines.Count} |");
        }
    }
}
