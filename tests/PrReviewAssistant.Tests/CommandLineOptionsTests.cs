using PrReviewAssistant.Providers;
using PrReviewAssistant.Review;

namespace PrReviewAssistant.Tests;

public class CommandLineOptionsTests
{
    [Fact]
    public void TryParse_OnlyDiff_UsesDefaults()
    {
        var ok = CommandLineOptions.TryParse(["--diff", "changes.diff"], out var options, out _);

        Assert.True(ok);
        Assert.Equal("changes.diff", options!.DiffPath);
        Assert.Equal("review.md", options.OutputPath);
        Assert.Equal("simulated", options.ProviderName);
    }

    [Fact]
    public void TryParse_AllOptions_AreRead()
    {
        string[] args = ["--diff", "a.diff", "--repo", "App", "--branch", "main", "--out", "out/r.md", "--provider", "simulated"];

        CommandLineOptions.TryParse(args, out var options, out _);

        Assert.Equal(new CommandLineOptions("a.diff", "App", "main", "out/r.md", "simulated", ShowHelp: false), options);
    }

    [Theory]
    [InlineData(new string[0], "--diff is required")]
    [InlineData(new[] { "--diff" }, "expected '--option value'")]
    [InlineData(new[] { "--diff", "a.diff", "--colour", "red" }, "unknown option")]
    [InlineData(new[] { "--diff", "a.diff", "--provider", "nope" }, "unknown provider")]
    public void TryParse_BadInput_ReturnsError(string[] args, string expectedError)
    {
        var ok = CommandLineOptions.TryParse(args, out _, out var error);

        Assert.False(ok);
        Assert.Contains(expectedError, error);
    }

    [Fact]
    public void Factory_CreatesSimulatedProvider()
    {
        Assert.IsType<SimulatedReviewProvider>(ReviewProviderFactory.Create("simulated"));
    }

    [Fact]
    public void Factory_UnknownName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ReviewProviderFactory.Create("claude"));
    }
}
