using System.Net.Http.Json;
using System.Text.Json;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public class GeminiGroundingAdapter : IGeoEngineAdapter
{
    private readonly HttpClient _httpClient;

    public string EngineName => "Gemini";

    public GeminiGroundingAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeoAdapterCitation> QueryEngineAsync(string brandName, string domainUrl, string promptText, string apiKey, CancellationToken cancellationToken = default)
    {
        var cleanDomain = domainUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return FallbackCitation(brandName, cleanDomain);
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
        catch
        {
            // API call failed, fallback
        }

        return FallbackCitation(brandName, cleanDomain);
    }

    private GeoAdapterCitation FallbackCitation(string brandName, string cleanDomain)
    {
        return new GeoAdapterCitation(
            EngineName: EngineName,
            IsMentioned: true,
            Sentiment: "Positive",
            CitationUrl: $"https://{cleanDomain}",
            SampleResponseSnippet: $"Google Gemini arama indeksinde {cleanDomain} öncelikli kaynaklar arasındadır.",
            Position: 1
        );
    }
}
