using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public class AnthropicAdapter : IGeoEngineAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicAdapter> _logger;

    public string EngineName => "Claude";
    public AiProviderEnum Provider => AiProviderEnum.Anthropic;

    public AnthropicAdapter(HttpClient httpClient, ILogger<AnthropicAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeoAdapterCitation> QueryEngineAsync(
        string brandName,
        string domainUrl,
        string promptText,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var cleanDomain = domainUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return NoResultCitation(
                DataSources.NotConfigured,
                $"No Anthropic API key is configured for this tenant, so {EngineName} was not queried.");
        }

        try
        {
            var requestUri = "https://api.anthropic.com/v1/messages";
            var payload = new
            {
                model = "claude-3-5-haiku-20241022",
                max_tokens = 600,
                messages = new[]
                {
                    new { role = "user", content = promptText }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var contentArray = root.GetProperty("content");
                string content = "";
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "text" &&
                        block.TryGetProperty("text", out var textProp))
                    {
                        content += textProp.GetString() + " ";
                    }
                }
                content = content.Trim();

                var citationUrl = "";
                if (content.Contains(cleanDomain, StringComparison.OrdinalIgnoreCase))
                {
                    citationUrl = $"https://{cleanDomain}";
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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _logger.LogWarning(ex, "{Engine} query failed for {Domain}.", EngineName, cleanDomain);
        }

        return NoResultCitation(
            DataSources.Unavailable,
            $"{EngineName} could not be reached for this run.");
    }

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
