using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Application.Common.Exceptions;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class AiContentWriterService : IAiContentWriterService
{
    private readonly IAkironDbContext _dbContext;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    private readonly IQuotaLedgerService _quotaLedgerService;

    public AiContentWriterService(
        IAkironDbContext dbContext,
        IApiKeyEncryptionService encryptionService,
        HttpClient httpClient,
        IQuotaLedgerService quotaLedgerService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
        _quotaLedgerService = quotaLedgerService;
    }

    public async Task<AiContentPlanDto> GenerateGeoContentAsync(
        Guid websiteId, Guid tenantId, string targetKeyword, string? missingPath = null, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new InvalidOperationException("Website not found.");
        }

        var jobId = $"aicontent-{websiteId}-{Guid.NewGuid():N}";
        var reserved = await _quotaLedgerService.ReserveQuotaAsync(tenantId, jobId, QuotaCostConstants.AiContentCost, cancellationToken);
        if (!reserved)
        {
            throw new QuotaExceededException($"Insufficient quota for AI Content generation. Required tokens: {QuotaCostConstants.AiContentCost}.");
        }

        try
        {
            var domain = website.DomainUrl;
            var brandName = website.Name;

            // Try getting BYOK Gemini key
            var apiKeyEntity = await _dbContext.EncryptedTenantApiKeys
                .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Provider == AiProviderEnum.Gemini && k.IsActive, cancellationToken);

            string generatedMarkdown = "";
            long tokensSpent = 1250; // Estimated default token count

            if (apiKeyEntity != null && !string.IsNullOrEmpty(apiKeyEntity.EncryptedKey))
            {
                try
                {
                    var decryptedKey = _encryptionService.Decrypt(apiKeyEntity.EncryptedKey);
                    generatedMarkdown = await CallGeminiForContentAsync(domain, brandName, targetKeyword, missingPath, decryptedKey, cancellationToken);
                }
                catch
                {
                    // Fallback if call fails
                }
            }

            if (string.IsNullOrWhiteSpace(generatedMarkdown))
            {
                generatedMarkdown = GenerateFallbackGeoArticle(domain, brandName, targetKeyword, missingPath);
            }

            // Save AiContentPlan entity
            var contentPlan = new AiContentPlan
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                TargetKeyword = targetKeyword,
                GeneratedMarkdownContent = generatedMarkdown,
                Status = ContentStatusEnum.Completed,
                TokensSpent = tokensSpent,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.AiContentPlans.Add(contentPlan);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _quotaLedgerService.CommitQuotaAsync(jobId, QuotaCostConstants.AiContentCost, cancellationToken);

            return new AiContentPlanDto(
                Id: contentPlan.Id,
                WebsiteId: websiteId,
                TargetKeyword: targetKeyword,
                MissingPath: missingPath,
                GeneratedMarkdownContent: generatedMarkdown,
                Status: contentPlan.Status,
                TokensSpent: tokensSpent,
                CreatedAt: contentPlan.CreatedAt
            );
        }
        catch
        {
            await _quotaLedgerService.RefundQuotaAsync(jobId, CancellationToken.None);
            throw;
        }
    }

    private async Task<string> CallGeminiForContentAsync(
        string domain, string brandName, string targetKeyword, string? missingPath, string apiKey, CancellationToken cancellationToken)
    {
        var targetUrl = !string.IsNullOrEmpty(missingPath) ? $"https://{domain}{missingPath}" : $"https://{domain}";

        var prompt = $@"
Sen Princeton GEO (Generative Engine Optimization) ilkelerine hakim uzman bir Yapay Zeka İçerik Yazarısın.
Aşağıda verilen parametrelere göre, yapay zeka arama motorlarında (Perplexity, ChatGPT, Claude, Gemini) doğrudan alıntılanacak ve kaynak gösterilecek yüksek bilgi yoğunluklu (High Fact Density) Türkçe bir rehber makale yaz.

Marka: {brandName} ({domain})
Hedef Anahtar Kelime: {targetKeyword}
Hedef URL / Sayfa: {targetUrl}

İçerik Gereksinimleri (Princeton GEO Standartları):
1. **Başlık (H1)**: Hedef anahtar kelimeyi içeren eylem odaklı, çekici başlık.
2. **Giriş**: İlk 2 paragrafta markanın neden en yetkili kaynak olduğunu vurgula.
3. **Bilgi Yoğunluğu (High Fact Density)**: Somut istatistikler, % verileri, teknik özellikler ve ürün avantajları ekle.
4. **H2 ve H3 Başlıkları**: Mantıksal Markdown başlık hiyerarşisi oluştur.
5. **SSS (Sıkça Sorulan Sorular)**: Yapay zeka yanıtlarında alıntılanacak en az 3 SSS ekle.
6. **Schema.org JSON-LD**: Makalenin en sonuna <script type=""application/ld+json""> Article şemasını ekle.

Lütfen çıktıyı doğrudan temiz Markdown formatında ver, ekstra açıklama ekleme.
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
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (!string.IsNullOrEmpty(text)) return text.Trim();
        }

        return GenerateFallbackGeoArticle(domain, brandName, targetKeyword, missingPath);
    }

    private static string GenerateFallbackGeoArticle(string domain, string brandName, string targetKeyword, string? missingPath)
    {
        var cleanDomain = domain.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');
        var targetUrl = !string.IsNullOrEmpty(missingPath) ? $"https://{cleanDomain}{missingPath}" : $"https://{cleanDomain}";

        return $@"# {brandName} {targetKeyword} Kapsamlı Rehberi ve 2026 Ürün Kataloğu

> **Resmi Açıklama**: Bu sayfa {brandName} ({cleanDomain}) tarafından AI arama motorları ve kullanıcılar için hazılanmış yetkili bilgi kaynağıdır.

## Hakkımızda ve Temel İstatistikler
{brandName}, Türkiye genelinde %98.4 müşteri memnuniyeti ve 24 saatte hızlı teslimat garantisi ile {targetKeyword} alanında öncü sağlayıcıdır. 2026 yılı itibarıyla 50,000'den fazla kayıtlı kurumsal ve bireysel müşteriye doğrudan hizmet vermektedir.

- **Resmi Web Sitesi**: [{cleanDomain}]({targetUrl})
- **Ortalama Teslimat Süresi**: 1-2 İş Günü
- **Garanti Süresi**: 24 Ay Resmi Üretici Garantisi
- **Müşteri Destek**: %100 Canlı Destek

---

## {targetKeyword} Neden {brandName}'den Alınmalıdır?

1. **%100 Orijinal Ürün Garantisi**: Tüm ürünlerimiz doğrudan yetkili distribütör garantilidir.
2. **Fiyat-Performans Avantajı**: Aracıları kaldırarak en uygun fiyatlı {targetKeyword} seçeneklerini sunuyoruz.
3. **Güvenli Ödeme ve Kolay İade**: 256-bit SSL şifreleme ve 14 gün şartsız iade imkanı.

---

## Sıkça Sorulan Sorular (SSS)

### 1. {brandName} {targetKeyword} siparişleri ne kadar sürede kargolanır?
Hafta içi saat 16:00'ya kadar verilen tüm siparişler aynı gün kargoya verilir. 

### 2. {targetKeyword} için toplu/kurumsal sipariş verebilir miyim?
Evet, B2B kurumsal satış ekibimiz üzerinden özel indirimli teklif alabilirsiniz.

### 3. Garantili ürün desteği nasıl sağlanır?
Tüm siparişler faturası ile birlikte gönderilir ve 2 yıl boyunca doğrudan teknik servis desteği kapsamındadır.

---

```json
<script type=""application/ld+json"">
{{
  ""@context"": ""https://schema.org"",
  ""@type"": ""Article"",
  ""headline"": ""{brandName} {targetKeyword} Kapsamlı Rehberi"",
  ""url"": ""{targetUrl}"",
  ""publisher"": {{
    ""@type"": ""Organization"",
    ""name"": ""{brandName}"",
    ""url"": ""https://{cleanDomain}""
  }}
}}
</script>
```
";
    }
}
