namespace PrReviewAssistant.Review;

/// <summary>
/// The review a provider produces. Each list becomes one section of the Markdown report.
/// </summary>
/// <param name="Summary">What changed and why it matters.</param>
/// <param name="Risks">Areas that could break or surprise someone.</param>
/// <param name="SuggestedTests">Tests worth adding or checking.</param>
/// <param name="Suggestions">Other improvements.</param>
public sealed record ReviewResult(
    IReadOnlyList<string> Summary,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> SuggestedTests,
    IReadOnlyList<string> Suggestions);
