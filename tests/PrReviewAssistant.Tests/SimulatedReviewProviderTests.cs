using PrReviewAssistant.Changes;
using PrReviewAssistant.Providers;
using PrReviewAssistant.Review;

namespace PrReviewAssistant.Tests;

public class SimulatedReviewProviderTests
{
    private readonly SimulatedReviewProvider _provider = new();

    private Task<ReviewResult> ReviewAsync(string diff) =>
        _provider.ReviewAsync(DiffParser.Parse(diff, "repo", "branch"));

    [Fact]
    public async Task GuardClause_IsSummarisedWithTheRightMethodAndFlaggedAsBehaviourChange()
    {
        var review = await ReviewAsync(SampleDiffs.MinorFix);

        // git's header says "Add", but the guard is inside Divide.
        Assert.Contains(review.Summary, s => s.Contains("guard to `MathService.Divide`") && s.Contains("DivideByZeroException"));
        Assert.Contains(review.Risks, r => r.StartsWith("Behaviour change in `MathService.Divide`"));
        Assert.Contains(review.SuggestedTests, t => t.Contains("boundary tests"));
    }

    [Fact]
    public async Task NewUnitTests_AreCounted()
    {
        var review = await ReviewAsync(SampleDiffs.MinorFix);

        Assert.Contains("Adds 1 unit test in `MathServiceTests.cs`.", review.Summary);
    }

    [Fact]
    public async Task CodeChangeWithMatchingTests_DoesNotAskForTests()
    {
        var review = await ReviewAsync(SampleDiffs.MinorFix);

        Assert.DoesNotContain(review.SuggestedTests, t => t.StartsWith("No test changes"));
    }

    [Fact]
    public async Task CodeChangeWithoutTests_AsksForTests()
    {
        var review = await ReviewAsync(SampleDiffs.SingleFile("src/OrderService.cs", "    var x = 1;"));

        Assert.Contains(review.SuggestedTests, t => t.StartsWith("No test changes accompany `OrderService.cs`"));
    }

    [Theory]
    [InlineData("    public async void Send() { }", "async void")]
    [InlineData("    var r = client.GetAsync(url).Result;", "blocking on a task")]
    [InlineData("    catch (Exception ex)", "catching every exception")]
    public async Task RiskyCode_IsFlagged(string addedLine, string expectedReason)
    {
        var review = await ReviewAsync(SampleDiffs.SingleFile("src/OrderService.cs", addedLine));

        Assert.Contains(review.Risks, r => r.Contains(expectedReason));
    }

    [Fact]
    public async Task HardCodedSecret_IsFlaggedWithoutRepeatingTheValue()
    {
        var review = await ReviewAsync(SampleDiffs.SingleFile(
            "src/OrderService.cs", "    private const string ApiKey = \"demo-key-123\";"));

        var risk = Assert.Single(review.Risks, r => r.Contains("hard-coded secret"));
        Assert.DoesNotContain("demo-key-123", risk);
        Assert.Contains("\"***\"", risk);
    }

    [Fact]
    public async Task ChangedPublicSignature_IsFlagged()
    {
        const string diff = """
            diff --git a/src/OrderService.cs b/src/OrderService.cs
            --- a/src/OrderService.cs
            +++ b/src/OrderService.cs
            @@ -1,1 +1,1 @@
            -    public decimal GetTotal(int orderId)
            +    public decimal GetTotal(int orderId, string currency)
            """;

        var review = await ReviewAsync(diff);

        Assert.Contains(review.Risks, r => r.Contains("Public member changed or removed") && r.Contains("GetTotal(int orderId)"));
    }

    [Fact]
    public async Task CleanChange_HasNoRisks()
    {
        var review = await ReviewAsync(SampleDiffs.SingleFile("src/Calculator.cs", "    var total = a + b;"));

        Assert.Empty(review.Risks);
    }

    [Fact]
    public async Task RenameAndBinaryFile_AreDescribedCorrectly()
    {
        var review = await ReviewAsync(SampleDiffs.RenameAndBinary);

        Assert.Contains("Adds binary file `logo.png`.", review.Summary);
        Assert.Contains("Renames `src/MathService.cs` to `src/Calculator.cs` (content unchanged).", review.Summary);
        Assert.Contains(review.Risks, r => r.Contains("is renamed to `Calculator.cs`"));
    }

    [Fact]
    public async Task PureRename_DoesNotAskForTests()
    {
        var review = await ReviewAsync(SampleDiffs.RenameAndBinary);

        Assert.Empty(review.SuggestedTests);
    }

    [Fact]
    public async Task EmptyChangeSet_SaysThereIsNothingToReview()
    {
        var review = await ReviewAsync("");

        Assert.Equal(["The diff contains no file changes."], review.Summary);
    }
}
