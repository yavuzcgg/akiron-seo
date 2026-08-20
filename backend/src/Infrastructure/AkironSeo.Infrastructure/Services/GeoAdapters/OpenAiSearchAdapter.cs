using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services.GeoAdapters;

public class OpenAiSearchAdapter : IGeoEngineAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiSearchAdapter> _logger;

    public string EngineName => "ChatGPT";
    public AiProviderEnum Provider => AiProviderEnum.OpenAI;

    public OpenAiSearchAdapter(HttpClient httpClient, ILogger<OpenAiSearchAdapter> logger)
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
                $"No OpenAI API key is configured for this tenant, so {EngineName} was not queried.");
        }

        try
        {
            var requestUri = "https://api.openai.com/v1/chat/completions";
            var payload = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are a comprehensive web search and recommendation assistant. Analyze the query objectively and list notable brand providers with their official website domains." },
                    new { role = "user", content = promptText }
                },
                max_tokens = 600,
                temperature = 0.3
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

                var choices = root.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

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
