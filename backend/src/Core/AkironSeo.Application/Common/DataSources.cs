namespace AkironSeo.Application.Common;

/// <summary>
/// Describes where a reported metric actually came from.
///
/// Several analytics surfaces do not yet have a third-party integration behind them.
/// Rather than presenting their output as a measurement, every result carries its
/// provenance so the UI can label it and so a reader of the API can tell the
/// difference. When a real integration lands, its service starts returning
/// <see cref="Live"/> and the label disappears everywhere at once.
/// </summary>
public static class DataSources
{
    /// <summary>A real third-party call succeeded and this is its parsed result.</summary>
    public const string Live = "Live";

    /// <summary>The tenant has not supplied an API key for this provider, so nothing was queried.</summary>
    public const string NotConfigured = "NotConfigured";

    /// <summary>A call was attempted and failed; the result is unknown, not negative.</summary>
    public const string Unavailable = "Unavailable";

    /// <summary>Synthetic output. No integration exists for this metric yet.</summary>
    public const string Simulated = "Simulated";
}
