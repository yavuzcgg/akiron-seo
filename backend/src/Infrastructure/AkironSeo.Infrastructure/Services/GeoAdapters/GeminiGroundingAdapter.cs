using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public class GeminiGroundingAdapter : IGeoEngineAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiGroundingAdapter> _logger;

    public string EngineName => "Gemini";
    public AiProviderEnum Provider => AiProviderEnum.Gemini;

    public GeminiGroundingAdapter(HttpClient httpClient, ILogger<GeminiGroundingAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeoAdapterCitation> QueryEngineAsync(string brandName, string domainUrl, string promptText, string apiKey, CancellationToken cancellationToken = default)
    {
        var cleanDomain = domainUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return NoResultCitation(
                DataSources.NotConfigured,
                $"No Gemini API key is configured for this tenant, so {EngineName} was not queried.");
        }

        try
        {
            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = promptText } } }
                },
                tools = new[]
                {
                    new { google_search = new { } }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(requestUri, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var candidate = doc.RootElement.GetProperty("candidates")[0];

                var text = candidate.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";

                var citationUrl = "";
                // Try parsing groundingMetadata web search sources
                if (candidate.TryGetProperty("groundingMetadata", out var grounding))
                {
                    if (grounding.TryGetProperty("groundingChunks", out var chunks))
                    {
                        foreach (var chunk in chunks.EnumerateArray())
                        {
                            if (chunk.TryGetProperty("web", out var webElem) && webElem.TryGetProperty("uri", out var uriElem))
                            {
                                var uri = uriElem.GetString() ?? "";
                                if (uri.Contains(cleanDomain, StringComparison.OrdinalIgnoreCase))
                                {
                                    citationUrl = uri;
                                    break;
                                }
                            }
                        }
                    }
                }

                bool isMentioned = text.Contains(brandName, StringComparison.OrdinalIgnoreCase) ||
                                  text.Contains(cleanDomain, StringComparison.OrdinalIgnoreCase) ||
                                  !string.IsNullOrEmpty(citationUrl);

                if (string.IsNullOrEmpty(citationUrl) && isMentioned)
                {
                    citationUrl = $"https://{cleanDomain}";
                }

                string snippet = text.Length > 200 ? text[..200] + "..." : text;

                return new GeoAdapterCitation(
                    EngineName: EngineName,
                    IsMentioned: isMentioned,
                    Sentiment: isMentioned ? "Positive" : "NotMentioned",
                    CitationUrl: citationUrl,
                    SampleResponseSnippet: snippet,
                    Position: isMentioned ? 1 : null
                );
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _logger.LogWarning(ex, "{Engine} query failed for {Domain}.", EngineName, cleanDomain);
        }

        return NoResultCitation(
            DataSources.Unavailable,
            $"{EngineName} could not be reached for this run.");
    }

    /// <summary>
    /// Used when the engine was not queried or did not answer. Reports "not mentioned"
    /// with the reason attached — never a fabricated positive citation, which would
    /// inflate share of voice for a tenant that has configured nothing.
    /// </summary>
    private GeoAdapterCitation NoResultCitation(string dataSource, string reason)
    {
        return new GeoAdapterCitation(
            EngineName: EngineName,
            IsMentioned: false,
            Sentiment: "Unknown",
            CitationUrl: string.Empty,
            SampleResponseSnippet: reason,
            Position: null,
            DataSource: dataSource
        );
    }
}
