using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class GeminiAiService : IAiOptimizationService
{
    private readonly IAkironDbContext _dbContext;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;

    public GeminiAiService(
        IAkironDbContext dbContext,
        IApiKeyEncryptionService encryptionService,
        HttpClient httpClient)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
    }

    public async Task<AiSeoRecommendationDto> GenerateSeoRecommendationsAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new InvalidOperationException("Website not found.");
        }

        var audit = await _dbContext.SeoAudits
            .Where(a => a.WebsiteId == websiteId && a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var crawlResult = audit != null
            ? await _dbContext.CrawlResults.FirstOrDefaultAsync(cr => cr.CrawlJobId == audit.CrawlJobId && cr.TenantId == tenantId, cancellationToken)
            : null;

        var title = crawlResult?.Title ?? website.Name;
        var metaDesc = crawlResult?.MetaDescription ?? "Henüz meta açıklaması bulunmamaktadır.";
        var domain = website.DomainUrl;

        // Try getting tenant's BYOK Gemini API Key
        var apiKeyEntity = await _dbContext.EncryptedTenantApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Provider == AiProviderEnum.Gemini && k.IsActive, cancellationToken);

        if (apiKeyEntity != null && !string.IsNullOrEmpty(apiKeyEntity.EncryptedKey))
        {
            try
            {
                var decryptedKey = _encryptionService.Decrypt(apiKeyEntity.EncryptedKey);
                return await CallGeminiApiAsync(domain, title, metaDesc, decryptedKey, cancellationToken);
            }
            catch
            {
                // Fallback if LLM API call fails
            }
        }

        // Default Rule-based Smart Recommendations when no BYOK key is configured
        return GenerateFallbackRecommendations(domain, title, metaDesc);
    }

    private async Task<AiSeoRecommendationDto> CallGeminiApiAsync(string domain, string currentTitle, string currentMetaDesc, string apiKey, CancellationToken cancellationToken)
    {
        var prompt = $@"
Sen uzman bir Türkçe SEO ve AEO (Answer Engine Optimization) danışmanısın.
Aşağıda verilen web sitesi bilgilerini incele ve tıklama oranını (CTR) ile arama sıralamasını artıracak Türkçe SEO önerileri sun.

Web Sitesi: {domain}
Mevcut Başlık: {currentTitle}
Mevcut Meta Açıklama: {currentMetaDesc}

Yanıtı sadece geçerli aşağıdaki JSON formatında ver, ekstra metin ekleme:
{{
  ""optimizedTitle"": ""30-60 karakter arası etkileyici Türkçe başlık"",
  ""optimizedMetaDescription"": ""120-160 karakter arası eylem odaklı Türkçe meta açıklama"",
  ""targetKeywords"": [""anahtar1"", ""anahtar2"", ""anahtar3""],
  ""actionableTips"": [
    ""Sitede H1 başlıklarını hedef kelimeye göre düzenleyin."",
    ""Schema.org Organization verisi ekleyin."",
    ""Sosyal medya OpenGraph görsel etiketlerini ekleyin.""
  ]
}}
";

        var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
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

                var optTitle = root.GetProperty("optimizedTitle").GetString() ?? $"{domain} | Özgün Ürünler & Hizmetler";
                var optMeta = root.GetProperty("optimizedMetaDescription").GetString() ?? $"{domain} adresinde kaliteli ürünler ve güvenli alışveriş imkanı.";
                
                var keywords = new List<string>();
                if (root.TryGetProperty("targetKeywords", out var kwElement))
                {
                    foreach (var k in kwElement.EnumerateArray()) keywords.Add(k.GetString() ?? "");
                }

                var tips = new List<string>();
                if (root.TryGetProperty("actionableTips", out var tipsElement))
                {
                    foreach (var t in tipsElement.EnumerateArray()) tips.Add(t.GetString() ?? "");
                }

                return new AiSeoRecommendationDto(optTitle, optMeta, keywords, tips);
            }
        }

        return GenerateFallbackRecommendations(domain, currentTitle, currentMetaDesc);
    }

    private static AiSeoRecommendationDto GenerateFallbackRecommendations(string domain, string currentTitle, string currentMetaDesc)
    {
        var cleanDomain = domain.Replace("https://", "").Replace("http://", "").Replace("www.", "").Split('/')[0];
        var brandName = char.ToUpper(cleanDomain[0]) + cleanDomain[1..];

        return new AiSeoRecommendationDto(
            OptimizedTitle: $"{brandName} | Kaliteli Ürünler & Resmi Satış Mağazası",
            OptimizedMetaDescription: $"{brandName} ile en yeni ürün gruplarını keşfedin. Hızlı kargo, uygun fiyat avantajı ve %100 güvenli online alışveriş imkanı.",
            TargetKeywords: new List<string> { brandName.ToLower(), "online sipariş", "en uygun fiyatlar", "motosiklet parçaları" },
            ActionableTips: new List<string>
            {
                "Başlık etiketini 30-60 karakter aralığında tutarak marka ve anahtar kelimenizi birleştirin.",
                "Meta açıklamasına eyleme çağrı (Call-to-Action) metinleri (Hızlı Kargo, Uygun Fiyat) ekleyin.",
                "BYOK menüsünden Gemini API Key kaydederek anlık AI önerilerini aktif edin."
            }
        );
    }
}
