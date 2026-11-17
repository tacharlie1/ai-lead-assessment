using System.Diagnostics.CodeAnalysis;
using PrReviewAssistant.Review;

namespace PrReviewAssistant;

/// <summary>The settings the user passes on the command line.</summary>
public sealed record CommandLineOptions(
    string DiffPath,
    string Repository,
    string Branch,
    string OutputPath,
    string ProviderName,
    bool ShowHelp)
{
    public static string Usage => $"""
        Usage:
          dotnet run --project src/PrReviewAssistant -- --diff <file> [options]

        Options:
          --diff <file>       Unified diff to review, e.g. from 'git diff main > changes.diff'. Required.
          --repo <name>       Repository name shown in the report. Default: local-repo
          --branch <name>     Branch name shown in the report. Default: working-changes
          --out <file>        Where to write the Markdown review. Default: review.md
          --provider <name>   Review provider. Available: {string.Join(", ", ReviewProviderFactory.AvailableNames)}. Default: simulated
          --help              Show this help.
        """;

    private static readonly HashSet<string> KnownOptions =
        new(["diff", "repo", "branch", "out", "provider"], StringComparer.OrdinalIgnoreCase);

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out CommandLineOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        options = null;
        error = null;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var name = args[i];

            if (name is "--help" or "-h")
            {
                options = new CommandLineOptions("", "", "", "", "", ShowHelp: true);
                return true;
            }

            if (!name.StartsWith("--") || i + 1 >= args.Length)
            {
                error = $"expected '--option value', got '{name}'.";
                return false;
            }

            var key = name[2..];
            if (!KnownOptions.Contains(key))
            {
                error = $"unknown option '{name}'.";
                return false;
            }

            values[key] = args[++i];
        }

        if (!values.TryGetValue("diff", out var diffPath))
        {
            error = "--diff is required.";
            return false;
        }

        var provider = values.GetValueOrDefault("provider", "simulated");
        if (!ReviewProviderFactory.AvailableNames.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            error = $"unknown provider '{provider}'. Available: {string.Join(", ", ReviewProviderFactory.AvailableNames)}.";
            return false;
        }

        options = new CommandLineOptions(
            DiffPath: diffPath,
            Repository: values.GetValueOrDefault("repo", "local-repo"),
            Branch: values.GetValueOrDefault("branch", "working-changes"),
            OutputPath: values.GetValueOrDefault("out", "review.md"),
            ProviderName: provider,
            ShowHelp: false);
        return true;
    }
}
