using PrReviewAssistant;
using PrReviewAssistant.Changes;
using PrReviewAssistant.Output;
using PrReviewAssistant.Review;

// Local PR review assistant.
// Reads a diff, asks a review provider for a review, and writes a Markdown file.
// Nothing here calls the network or GitHub.

if (!CommandLineOptions.TryParse(args, out var options, out var error))
{
    Console.Error.WriteLine($"Error: {error}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(CommandLineOptions.Usage);
    return 1;
}

if (options.ShowHelp)
{
    Console.WriteLine(CommandLineOptions.Usage);
    return 0;
}

if (!File.Exists(options.DiffPath))
{
    Console.Error.WriteLine($"Error: diff file not found: {options.DiffPath}");
    return 1;
}

// 1. Read the change set from the diff.
var diffText = await File.ReadAllTextAsync(options.DiffPath);
var changes = DiffParser.Parse(diffText, options.Repository, options.Branch);

// 2. Ask the chosen provider for a review. This is the extension point: swap the provider by name.
var provider = ReviewProviderFactory.Create(options.ProviderName);
var review = await provider.ReviewAsync(changes);

// 3. Write the Markdown review for a human reviewer.
var markdown = MarkdownReportWriter.Render(changes, review, provider.Name, DateTimeOffset.Now);
var outputPath = Path.GetFullPath(options.OutputPath);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(outputPath, markdown);

Console.WriteLine($"Reviewed {changes.Files.Count} file(s) with the '{provider.Name}' provider.");
Console.WriteLine($"Review written to {outputPath}");
return 0;
