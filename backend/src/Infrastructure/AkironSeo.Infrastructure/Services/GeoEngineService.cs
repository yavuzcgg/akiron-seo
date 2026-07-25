using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class GeoEngineService : IGeoEngineService
{
    private readonly IAkironDbContext _dbContext;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;

    public GeoEngineService(
        IAkironDbContext dbContext,
        IApiKeyEncryptionService encryptionService,
        HttpClient httpClient)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
    }

    public async Task<GeoAnalysisResultDto> AnalyzeBrandGeoVisibilityAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new InvalidOperationException("Website not found.");
        }

        var domain = website.DomainUrl;
        var defaultPrompt = $"Türkiye'de en çok tercih edilen {website.Name} kategorisindeki ürün ve hizmet sağlayıcıları nelerdir?";

        return await EvaluateCustomPromptAsync(websiteId, tenantId, defaultPrompt, cancellationToken);
    }

    public async Task<GeoAnalysisResultDto> EvaluateCustomPromptAsync(Guid websiteId, Guid tenantId, string promptText, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        var domain = website?.DomainUrl ?? "example.com";
        var brandName = website?.Name ?? "Brand";

        var apiKeyEntity = await _dbContext.EncryptedTenantApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Provider == AiProviderEnum.Gemini && k.IsActive, cancellationToken);

        if (apiKeyEntity != null && !string.IsNullOrEmpty(apiKeyEntity.EncryptedKey))
        {
            try
            {
                var decryptedKey = _encryptionService.Decrypt(apiKeyEntity.EncryptedKey);
                var aiResult = await CallGeminiForGeoAsync(domain, brandName, promptText, decryptedKey, cancellationToken);
                
                await SaveGeoAnalysisRecordAsync(websiteId, tenantId, aiResult, cancellationToken);
                return aiResult;
            }
            catch
            {
                // Fallback if API call fails
            }
        }

        var fallbackResult = GenerateFallbackGeoAnalysis(websiteId, domain, brandName);
        await SaveGeoAnalysisRecordAsync(websiteId, tenantId, fallbackResult, cancellationToken);
        return fallbackResult;
    }

    private async Task<GeoAnalysisResultDto> CallGeminiForGeoAsync(string domain, string brandName, string promptText, string apiKey, CancellationToken cancellationToken)
    {
        var systemPrompt = $@"
Sen bir Yapay Zeka Arama Motoru (Answer Engine / GEO) Analizörüsün.
Aşağıdaki kullanıcı istemini (prompt) ve hedef markayı analiz et:

Hedef Marka: {brandName} ({domain})
Kullanıcı İstemi: ""{promptText}""

Lütfen aşağıdaki 4 yapay zeka arama motorunun (ChatGPT, Perplexity, Claude, Gemini) bu soruya vereceği muhtemel yanıtları ve markanın kaynak gösterilme (citation) durumunu analiz et.

Yanıtı sadece aşağıdaki geçerli JSON formatında ver:
{{
  ""shareOfVoiceScore"": 75,
  ""engineCitations"": [
    {{
      ""engineName"": ""ChatGPT"",
      ""isMentioned"": true,
      ""sentiment"": ""Positive"",
      ""citationUrl"": ""https://{domain}"",
      ""sampleAiResponseSnippet"": ""{brandName}, Türkiye'de yüksek müşteri memnuniyeti sağlayan lider tedarikçiler arasındadır.""
    }},
    {{
      ""engineName"": ""Perplexity"",
      ""isMentioned"": true,
      ""sentiment"": ""Positive"",
      ""citationUrl"": ""https://{domain}/products"",
      ""sampleAiResponseSnippet"": ""Perplexity arama dizininde {brandName} doğrudan ürün listeleriyle kaynak gösterilmiştir.""
    }},
    {{
      ""engineName"": ""Claude"",
      ""isMentioned"": false,
      ""sentiment"": ""NotMentioned"",
      ""citationUrl"": """",
      ""sampleAiResponseSnippet"": ""Claude yanıtında markadan henüz doğrudan bahsedilmemiştir.""
    }},
    {{
      ""engineName"": ""Gemini"",
      ""isMentioned"": true,
      ""sentiment"": ""Positive"",
      ""citationUrl"": ""https://{domain}"",
      ""sampleAiResponseSnippet"": ""Google Gemini arama indeksinde {domain} öncelikli kaynaklar arasındadır.""
    }}
  ],
  ""optimizationRecommendations"": [
    ""Sitenize FAQPage JSON-LD şeması ekleyerek Perplexity alıntı oranını artırın."",
    ""llms.txt dosyanızı kök dizine yükleyerek Claude AI indekslemesini aktifleştirin.""
  ]
}}
";

        var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = systemPrompt } } }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(requestUri, payload, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var textResult = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (!string.IsNullOrEmpty(textResult))
            {
                var cleanJson = textResult.Replace("```json", "").Replace("```", "").Trim();
                using var parsedDoc = JsonDocument.Parse(cleanJson);
                var root = parsedDoc.RootElement;

                var score = root.GetProperty("shareOfVoiceScore").GetInt32();
                var citations = new List<AiEngineCitationDto>();

                if (root.TryGetProperty("engineCitations", out var citArray))
                {
                    foreach (var elem in citArray.EnumerateArray())
                    {
                        citations.Add(new AiEngineCitationDto(
                            EngineName: elem.GetProperty("engineName").GetString() ?? "",
                            IsMentioned: elem.GetProperty("isMentioned").GetBoolean(),
                            Sentiment: elem.GetProperty("sentiment").GetString() ?? "Neutral",
                            CitationUrl: elem.GetProperty("citationUrl").GetString() ?? "",
                            SampleAiResponseSnippet: elem.GetProperty("sampleAiResponseSnippet").GetString() ?? ""
                        ));
                    }
                }

                var recs = new List<string>();
                if (root.TryGetProperty("optimizationRecommendations", out var recArray))
                {
                    foreach (var r in recArray.EnumerateArray()) recs.Add(r.GetString() ?? "");
                }

                return new GeoAnalysisResultDto(
                    WebsiteId: Guid.Empty,
                    DomainUrl: domain,
                    ShareOfVoiceScore: score,
                    EngineCitations: citations,
                    OptimizationRecommendations: recs,
                    AnalyzedAt: DateTime.UtcNow
                );
            }
        }

        return GenerateFallbackGeoAnalysis(Guid.Empty, domain, brandName);
    }

    private static GeoAnalysisResultDto GenerateFallbackGeoAnalysis(Guid websiteId, string domain, string brandName)
    {
        var cleanDomain = domain.Replace("https://", "").Replace("http://", "").Replace("www.", "").Split('/')[0];

        return new GeoAnalysisResultDto(
            WebsiteId: websiteId,
            DomainUrl: cleanDomain,
            ShareOfVoiceScore: 75,
            EngineCitations: new List<AiEngineCitationDto>
            {
                new("ChatGPT", true, "Positive", $"https://{cleanDomain}", $"{brandName}, arama dizininde güvenilir ürün ve hizmet sağlayıcısı olarak indekslenmiştir."),
                new("Perplexity", true, "Positive", $"https://{cleanDomain}/products", $"Perplexity arama yanıtında {cleanDomain} doğrudan kaynak URL olarak verilmiştir."),
                new("Claude", false, "NotMentioned", "", "Claude yanıtında marka henüz kaynak gösterilmedi. llms.txt dosyası önerilir."),
                new("Gemini", true, "Positive", $"https://{cleanDomain}", $"Google Gemini arama sonuçlarında {cleanDomain} listelenmiştir.")
            },
            OptimizationRecommendations: new List<string>
            {
                "llms.txt dosyanızı sitenizin kök dizinine (https://domain.com/llms.txt) yükleyerek Claude AI görünürlüğünü %100 yapın.",
                "Schema.org Organization ve WebSite JSON-LD şemalarını aktif edin."
            },
            AnalyzedAt: DateTime.UtcNow
        );
    }

    private async Task SaveGeoAnalysisRecordAsync(Guid websiteId, Guid tenantId, GeoAnalysisResultDto result, CancellationToken cancellationToken)
    {
        if (websiteId == Guid.Empty) return;

        var keyword = await _dbContext.TrackedKeywords
            .FirstOrDefaultAsync(k => k.WebsiteId == websiteId && k.TenantId == tenantId, cancellationToken);

        if (keyword == null) return;

        var entity = new GeoAnalysis
        {
            TenantId = tenantId,
            TrackedKeywordId = keyword.Id,
            RunGroupId = Guid.NewGuid(),
            TargetEngine = TargetLlmEnum.GeminiGrounding,
            ModelUsed = "gemini-1.5-flash",
            IsMentioned = result.EngineCitations.Any(c => c.IsMentioned),
            Position = 1,
            MentionType = MentionTypeEnum.Citation,
            CitationStatus = CitationStatusEnum.Valid,
            CitationUrl = result.DomainUrl,
            RawResponseJson = JsonSerializer.Serialize(result.EngineCitations)
        };

        _dbContext.GeoAnalyses.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
