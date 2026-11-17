using PrReviewAssistant.Changes;

namespace PrReviewAssistant.Review;

/// <summary>
/// The extension point. Anything that can turn a change set into a review implements this interface:
/// the built-in simulated provider today, a Claude adapter later. The rest of the tool never needs to
/// know which provider is in use.
/// </summary>
public interface IReviewProvider
{
    /// <summary>Short name used on the command line, e.g. "simulated".</summary>
    string Name { get; }

    Task<ReviewResult> ReviewAsync(ChangeSet changes, CancellationToken cancellationToken = default);
}
