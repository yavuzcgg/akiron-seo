using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public class PerplexitySonarAdapter : IGeoEngineAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PerplexitySonarAdapter> _logger;

    public string EngineName => "Perplexity";
    public AiProviderEnum Provider => AiProviderEnum.Perplexity;

    public PerplexitySonarAdapter(HttpClient httpClient, ILogger<PerplexitySonarAdapter> logger)
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
                $"No Perplexity API key is configured for this tenant, so {EngineName} was not queried.");
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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
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
