using PrReviewAssistant.Changes;

namespace PrReviewAssistant.Tests;

public class DiffParserTests
{
    [Fact]
    public void Parse_ReadsEachFileWithItsAddedAndRemovedLines()
    {
        var changes = DiffParser.Parse(SampleDiffs.MinorFix, "Sample.DotNetApp", "feature/minor-fix");

        Assert.Equal("Sample.DotNetApp", changes.Repository);
        Assert.Equal("feature/minor-fix", changes.Branch);
        Assert.Collection(changes.Files,
            code =>
            {
                Assert.Equal("src/Sample.DotNetApp/MathService.cs", code.Path);
                Assert.Equal(ChangeKind.Modified, code.Kind);
                Assert.Equal(5, code.AddedLines.Count);
                Assert.Empty(code.RemovedLines);
                Assert.False(code.IsTestFile);
            },
            test =>
            {
                Assert.Equal("tests/Sample.DotNetApp.Tests/MathServiceTests.cs", test.Path);
                Assert.Equal(6, test.AddedLines.Count);
                Assert.True(test.IsTestFile);
            });
    }

    [Fact]
    public void Parse_KeepsHunkHeaderAndContextLines()
    {
        var changes = DiffParser.Parse(SampleDiffs.MinorFix, "repo", "branch");
        var hunk = changes.Files[0].Hunks.Single();

        Assert.Equal("public double Add(double a, double b)", hunk.Header);
        Assert.Equal(DiffLineKind.Context, hunk.Lines[0].Kind);
        Assert.Contains("public double Divide", hunk.Lines[0].Text);
    }

    [Fact]
    public void Parse_DetectsAddedAndDeletedFiles()
    {
        const string diff = """
            diff --git a/src/New.cs b/src/New.cs
            new file mode 100644
            --- /dev/null
            +++ b/src/New.cs
            @@ -0,0 +1 @@
            +public class New { }
            diff --git a/src/Old.cs b/src/Old.cs
            deleted file mode 100644
            --- a/src/Old.cs
            +++ /dev/null
            @@ -1 +0,0 @@
            -public class Old { }
            """;

        var changes = DiffParser.Parse(diff, "repo", "branch");

        Assert.Equal(ChangeKind.Added, changes.Files[0].Kind);
        Assert.Equal(ChangeKind.Deleted, changes.Files[1].Kind);
        Assert.Equal(1, changes.TotalAdded);
        Assert.Equal(1, changes.TotalRemoved);
    }

    [Fact]
    public void Parse_DetectsRenamedAndBinaryFiles()
    {
        var changes = DiffParser.Parse(SampleDiffs.RenameAndBinary, "repo", "branch");

        var image = changes.Files[0];
        Assert.Equal("logo.png", image.Path);
        Assert.Equal(ChangeKind.Added, image.Kind);
        Assert.True(image.IsBinary);

        var renamed = changes.Files[1];
        Assert.Equal("src/Calculator.cs", renamed.Path);
        Assert.Equal("src/MathService.cs", renamed.OldPath);
        Assert.Equal(ChangeKind.Renamed, renamed.Kind);
        Assert.True(renamed.IsPureRename);
    }

    [Fact]
    public void Parse_EmptyDiff_ReturnsNoFiles()
    {
        var changes = DiffParser.Parse("", "repo", "branch");

        Assert.Empty(changes.Files);
    }
}
