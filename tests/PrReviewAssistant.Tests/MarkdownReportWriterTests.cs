using PrReviewAssistant.Changes;
using PrReviewAssistant.Output;
using PrReviewAssistant.Review;

namespace PrReviewAssistant.Tests;

public class MarkdownReportWriterTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 11, 17, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Render_WritesTitleSectionsAndFileTable()
    {
        var changes = DiffParser.Parse(SampleDiffs.MinorFix, "Sample.DotNetApp", "feature/minor-fix");
        var review = new ReviewResult(["Adds a guard."], ["Behaviour change."], ["Test the guard."], []);

        var markdown = MarkdownReportWriter.Render(changes, review, "simulated", FixedTime);

        Assert.StartsWith("# AI Review: Sample.DotNetApp (feature/minor-fix)", markdown);
        Assert.Contains("Provider: simulated", markdown);
        Assert.Contains("2026-11-17 09:30", markdown);
        Assert.Contains("## Summary\n\n- Adds a guard.", markdown.ReplaceLineEndings("\n"));
        Assert.Contains("## Potential Risks", markdown);
        Assert.Contains("## Suggested Tests", markdown);
        Assert.Contains("| `src/Sample.DotNetApp/MathService.cs` | Modified | 5 | 0 |", markdown);
    }

    [Fact]
    public void Render_EmptySection_SaysNoneFound()
    {
        var changes = DiffParser.Parse("", "repo", "branch");
        var review = new ReviewResult([], [], [], []);

        var markdown = MarkdownReportWriter.Render(changes, review, "simulated", FixedTime);

        Assert.Contains("## Suggestions\n\n- None found.", markdown.ReplaceLineEndings("\n"));
    }
}
