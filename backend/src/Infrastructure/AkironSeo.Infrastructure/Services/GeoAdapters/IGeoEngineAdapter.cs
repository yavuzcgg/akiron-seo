using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public record GeoAdapterCitation(
    string EngineName,
    bool IsMentioned,
    string Sentiment,
    string CitationUrl,
    string SampleResponseSnippet,
    int? Position
);

public interface IGeoEngineAdapter
{
    string EngineName { get; }
    Task<GeoAdapterCitation> QueryEngineAsync(string brandName, string domainUrl, string promptText, string apiKey, CancellationToken cancellationToken = default);
}
