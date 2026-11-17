using PrReviewAssistant.Providers;

namespace PrReviewAssistant.Review;

/// <summary>
/// Picks a review provider by name. To add a provider (for example a Claude adapter),
/// implement <see cref="IReviewProvider"/> and add one line to <see cref="Providers"/>.
/// </summary>
public static class ReviewProviderFactory
{
    private static readonly Dictionary<string, Func<IReviewProvider>> Providers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["simulated"] = () => new SimulatedReviewProvider(),
        };

    public static IReadOnlyCollection<string> AvailableNames => Providers.Keys;

    public static IReviewProvider Create(string name)
    {
        if (Providers.TryGetValue(name, out var create))
        {
            return create();
        }

        throw new ArgumentException(
            $"Unknown review provider '{name}'. Available: {string.Join(", ", AvailableNames)}.",
            nameof(name));
    }
}
