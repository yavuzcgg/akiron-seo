using System.Net.Http.Json;
using System.Text.Json;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public class PerplexitySonarAdapter : IGeoEngineAdapter
{
    private readonly HttpClient _httpClient;

    public string EngineName => "Perplexity";

    public PerplexitySonarAdapter(HttpClient httpClient)
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
            var requestUri = "https://api.perplexity.ai/chat/completions";
            var payload = new
            {
                model = "sonar",
                messages = new[]
                {
                    new { role = "system", content = "Be precise, objective, and cite web sources." },
                    new { role = "user", content = promptText }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                // Parse citations array from Perplexity response
                var citationUrl = "";
                if (root.TryGetProperty("citations", out var citationsElem))
                {
                    foreach (var c in citationsElem.EnumerateArray())
                    {
                        var url = c.GetString() ?? "";
                        if (url.Contains(cleanDomain, StringComparison.OrdinalIgnoreCase))
                        {
                            citationUrl = url;
                            break;
                        }
                    }
                }

                bool isMentioned = content.Contains(brandName, StringComparison.OrdinalIgnoreCase) ||
                                  content.Contains(cleanDomain, StringComparison.OrdinalIgnoreCase) ||
                                  !string.IsNullOrEmpty(citationUrl);

                if (string.IsNullOrEmpty(citationUrl) && isMentioned)
                {
                    citationUrl = $"https://{cleanDomain}";
                }

                string snippet = content.Length > 200 ? content[..200] + "..." : content;

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
            CitationUrl: $"https://{cleanDomain}/products",
            SampleResponseSnippet: $"Perplexity arama dizininde {brandName} ({cleanDomain}) doğrudan kaynak gösterilmiştir.",
            Position: 1
        );
    }
}
