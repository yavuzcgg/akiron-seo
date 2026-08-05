using AkironSeo.Application.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public record GeoAdapterCitation(
    string EngineName,
    bool IsMentioned,
    string Sentiment,
    string CitationUrl,
    string SampleResponseSnippet,
    int? Position,
    // See DataSources. A missing key or a failed call must never be reported as a mention.
    string DataSource = DataSources.Live
);

public interface IGeoEngineAdapter
{
    string EngineName { get; }

    /// <summary>Which tenant BYOK key this adapter needs, so the caller can resolve it generically.</summary>
    AiProviderEnum Provider { get; }

    Task<GeoAdapterCitation> QueryEngineAsync(string brandName, string domainUrl, string promptText, string apiKey, CancellationToken cancellationToken = default);
}
