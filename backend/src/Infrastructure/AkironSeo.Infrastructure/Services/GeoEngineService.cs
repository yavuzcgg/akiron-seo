using System.Security.Cryptography;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Services.GeoAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services;

public class GeoEngineService : IGeoEngineService
{
    private readonly IAkironDbContext _dbContext;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly ICitationVerificationService _citationVerifier;
    private readonly IEnumerable<IGeoEngineAdapter> _adapters;
    private readonly ILogger<GeoEngineService> _logger;

    public GeoEngineService(
        IAkironDbContext dbContext,
        IApiKeyEncryptionService encryptionService,
        ICitationVerificationService citationVerifier,
        IEnumerable<IGeoEngineAdapter> adapters,
        ILogger<GeoEngineService> logger)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _citationVerifier = citationVerifier;
        _adapters = adapters;
        _logger = logger;
    }

    public async Task<GeoAnalysisResultDto> AnalyzeBrandGeoVisibilityAsync(
        Guid websiteId, Guid tenantId, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new InvalidOperationException("Website not found.");
        }

        var defaultPrompt = $"Türkiye'de en çok tercih edilen {website.Name} kategorisindeki ürün ve hizmet sağlayıcıları nelerdir?";
        return await EvaluateCustomPromptAsync(websiteId, tenantId, defaultPrompt, forceRefresh, cancellationToken);
    }

    public async Task<GeoAnalysisResultDto> EvaluateCustomPromptAsync(
        Guid websiteId, Guid tenantId, string promptText, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        var domain = website?.DomainUrl ?? "example.com";
        var brandName = website?.Name ?? "Brand";

        // Check 24-hour cache unless forceRefresh is true
        if (!forceRefresh && websiteId != Guid.Empty)
        {
            var cachedAnalysis = await GetCachedAnalysisAsync(websiteId, tenantId, cancellationToken);
            if (cachedAnalysis != null)
            {
                return cachedAnalysis;
            }
        }

        // Resolve each adapter's tenant BYOK key generically from its declared provider.
        var activeKeys = await _dbContext.EncryptedTenantApiKeys
            .Where(k => k.TenantId == tenantId && k.IsActive)
            .ToListAsync(cancellationToken);

        // Multi-sample iteration engine (3 sample runs with jitter for Mention Rate %)
        const int sampleCount = 3;
        var engineCitationsMap = new Dictionary<string, List<GeoAdapterCitation>>();

        var adapters = _adapters
            .Select(adapter =>
            {
                var entity = activeKeys.FirstOrDefault(k => k.Provider == adapter.Provider);
                var key = entity is not null && !string.IsNullOrEmpty(entity.EncryptedKey)
                    ? DecryptKeySafe(entity.EncryptedKey)
                    : string.Empty;
                return (Adapter: adapter, Key: key);
            })
            .ToList();

        var random = new Random();

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            if (sampleIndex > 0)
            {
                // Jitter delay between sample iterations (200 - 400ms)
                await Task.Delay(random.Next(200, 400), cancellationToken);
            }

            foreach (var (adapter, key) in adapters)
            {
                var citation = await adapter.QueryEngineAsync(brandName, domain, promptText, key, cancellationToken);

                if (!engineCitationsMap.ContainsKey(adapter.EngineName))
                {
                    engineCitationsMap[adapter.EngineName] = new List<GeoAdapterCitation>();
                }
                engineCitationsMap[adapter.EngineName].Add(citation);
            }
        }

        // Process citations, calculate Mention Rate %, and verify URLs
        var finalCitations = new List<AiEngineCitationDto>();
        int liveMentionCount = 0;
        int liveEngineCount = 0;

        foreach (var (engineName, citations) in engineCitationsMap)
        {
            var representativeCitation = citations.FirstOrDefault(c => c.IsMentioned) ?? citations.First();

            // An engine that was never queried is no evidence either way, so it is left out
            // of the rate maths instead of counting as a zero.
            bool isLive = representativeCitation.DataSource == DataSources.Live;

            int mentionCount = citations.Count(c => c.IsMentioned);
            int mentionRate = isLive ? (int)Math.Round((double)mentionCount / sampleCount * 100) : 0;

            if (isLive)
            {
                liveEngineCount++;
                liveMentionCount += mentionCount;
            }

            // Verify citation URL if present
            string citationStatus = "Valid";
            bool isGoldOpportunity = false;
            string citationUrl = representativeCitation.CitationUrl;

            if (!string.IsNullOrWhiteSpace(citationUrl))
            {
                var verifyResult = await _citationVerifier.VerifyCitationUrlAsync(
                    citationUrl, domain, websiteId, tenantId, promptText, engineName, cancellationToken);

                citationStatus = verifyResult.Status.ToString();
                isGoldOpportunity = verifyResult.IsGoldOpportunity;
            }

            finalCitations.Add(new AiEngineCitationDto(
                EngineName: engineName,
                IsMentioned: isLive && mentionCount > 0,
                Sentiment: isLive ? (mentionCount > 0 ? "Positive" : "NotMentioned") : "Unknown",
                CitationUrl: citationUrl,
                SampleAiResponseSnippet: representativeCitation.SampleResponseSnippet,
                MentionRatePercentage: mentionRate,
                CitationStatus: citationStatus,
                IsGoldOpportunity: isGoldOpportunity,
                DataSource: representativeCitation.DataSource
            ));
        }

        int overallMentionRate = liveEngineCount > 0
            ? (int)Math.Round((double)liveMentionCount / (liveEngineCount * sampleCount) * 100)
            : 0;

        // Share of voice is only meaningful once at least one engine actually answered.
        int shareOfVoiceScore = liveEngineCount > 0 ? Math.Min(overallMentionRate + 15, 100) : 0;

        if (liveEngineCount == 0)
        {
            _logger.LogInformation(
                "GEO analysis for website {WebsiteId} produced no live engine results; no provider key is configured or every call failed.",
                websiteId);
        }

        var recommendations = GenerateRecommendations(finalCitations, domain, liveEngineCount);

        var result = new GeoAnalysisResultDto(
            WebsiteId: websiteId,
            DomainUrl: domain,
            ShareOfVoiceScore: shareOfVoiceScore,
            OverallMentionRatePercentage: overallMentionRate,
            EngineCitations: finalCitations,
            OptimizationRecommendations: recommendations,
            AnalyzedAt: DateTime.UtcNow,
            IsCached: false,
            LiveEngineCount: liveEngineCount
        );

        // Save DB record
        await SaveGeoAnalysisRecordsAsync(websiteId, tenantId, result, cancellationToken);

        return result;
    }

    private static List<string> GenerateRecommendations(
        List<AiEngineCitationDto> citations, string domain, int liveEngineCount)
    {
        var recs = new List<string>();

        if (liveEngineCount == 0)
        {
            recs.Add("No AI engine could be queried. Add a Perplexity or Gemini API key under BYOK settings to start measuring citations.");
            return recs;
        }

        var missingPages = citations.Where(c => c.CitationStatus == "NonExistentPage" || c.IsGoldOpportunity).ToList();
        if (missingPages.Count > 0)
        {
            recs.Add($"🌟 GOLD OPPORTUNITY: AI engine(s) [{string.Join(", ", missingPages.Select(m => m.EngineName))}] cited missing 404 pages on {domain}. Create these pages now using AI Content Writer!");
        }

        // Only engines that actually answered can be said to have omitted the brand.
        var unmentioned = citations
            .Where(c => c.DataSource == DataSources.Live && !c.IsMentioned)
            .ToList();
        if (unmentioned.Count > 0)
        {
            recs.Add($"Upload llms.txt to root directory (https://{domain}/llms.txt) to increase visibility in [{string.Join(", ", unmentioned.Select(u => u.EngineName))}].");
        }

        var notConfigured = citations
            .Where(c => c.DataSource == DataSources.NotConfigured)
            .ToList();
        if (notConfigured.Count > 0)
        {
            recs.Add($"Not measured: [{string.Join(", ", notConfigured.Select(c => c.EngineName))}]. Add the matching API key under BYOK settings to include them.");
        }

        recs.Add("Add Organization and FAQPage Schema.org JSON-LD scripts to your homepage.");
        recs.Add("Improve Princeton GEO high-fact-density content structure on key landing pages.");

        return recs;
    }

    private async Task<GeoAnalysisResultDto?> GetCachedAnalysisAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        // Scoped to the website, not just the tenant: without this a multi-website tenant
        // is served another site's analysis for 24 hours.
        var recentAnalysis = await _dbContext.GeoAnalyses
            .Where(g => g.TenantId == tenantId && g.WebsiteId == websiteId && g.CreatedAt >= cutoff)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentAnalysis == null) return null;

        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null) return null;

        var citations = new List<AiEngineCitationDto>();

        try
        {
            if (!string.IsNullOrEmpty(recentAnalysis.RawResponseJson))
            {
                citations = JsonSerializer.Deserialize<List<AiEngineCitationDto>>(recentAnalysis.RawResponseJson) ?? new List<AiEngineCitationDto>();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Cached GEO analysis {AnalysisId} could not be deserialized; recomputing.", recentAnalysis.Id);
            return null;
        }

        if (citations.Count == 0) return null;

        var liveCitations = citations.Where(c => c.DataSource == DataSources.Live).ToList();
        int liveEngineCount = liveCitations.Count;
        int overallMentionRate = liveEngineCount > 0
            ? (int)Math.Round((double)liveCitations.Count(c => c.IsMentioned) / liveEngineCount * 100)
            : 0;

        return new GeoAnalysisResultDto(
            WebsiteId: websiteId,
            DomainUrl: website.DomainUrl,
            ShareOfVoiceScore: liveEngineCount > 0 ? Math.Min(overallMentionRate + 15, 100) : 0,
            OverallMentionRatePercentage: overallMentionRate,
            EngineCitations: citations,
            OptimizationRecommendations: GenerateRecommendations(citations, website.DomainUrl, liveEngineCount),
            AnalyzedAt: recentAnalysis.CreatedAt,
            IsCached: true,
            LiveEngineCount: liveEngineCount
        );
    }

    private async Task SaveGeoAnalysisRecordsAsync(Guid websiteId, Guid tenantId, GeoAnalysisResultDto result, CancellationToken cancellationToken)
    {
        if (websiteId == Guid.Empty) return;

        var keyword = await _dbContext.TrackedKeywords
            .FirstOrDefaultAsync(k => k.WebsiteId == websiteId && k.TenantId == tenantId, cancellationToken);

        var runGroupId = Guid.NewGuid();

        foreach (var citation in result.EngineCitations)
        {
            var entity = new GeoAnalysis
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                // Null when the website has no tracked keyword. Previously Guid.Empty, which
                // violated the foreign key and made the whole endpoint return 500.
                TrackedKeywordId = keyword?.Id,
                RunGroupId = runGroupId,
                TargetEngine = TargetLlmEnum.GeminiGrounding,
                ModelUsed = citation.EngineName,
                IsMentioned = citation.IsMentioned,
                Position = citation.IsMentioned ? 1 : null,
                MentionType = MentionTypeEnum.Citation,
                CitationStatus = Enum.TryParse<CitationStatusEnum>(citation.CitationStatus, out var status) ? status : CitationStatusEnum.Valid,
                CitationUrl = citation.CitationUrl,
                RawResponseJson = JsonSerializer.Serialize(result.EngineCitations)
            };

            _dbContext.GeoAnalyses.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string DecryptKeySafe(string encryptedKey)
    {
        try
        {
            return _encryptionService.Decrypt(encryptedKey);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        {
            // Usually means the master encryption key was rotated after the tenant stored
            // this key. Treated as "no key" so the engine reports NotConfigured.
            _logger.LogWarning(ex, "A stored tenant API key could not be decrypted.");
            return "";
        }
    }
}
