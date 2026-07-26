namespace AkironSeo.Application.Common.Interfaces;

public record AiEngineCitationDto(
    string EngineName, // "ChatGPT", "Perplexity", "Claude", "Gemini"
    bool IsMentioned,
    string Sentiment, // "Positive", "Neutral", "NotMentioned"
    string CitationUrl,
    string SampleAiResponseSnippet,
    int MentionRatePercentage = 100,
    string CitationStatus = "Valid",
    bool IsGoldOpportunity = false
);

public record GeoAnalysisResultDto(
    Guid WebsiteId,
    string DomainUrl,
    int ShareOfVoiceScore, // 0 - 100%
    int OverallMentionRatePercentage,
    List<AiEngineCitationDto> EngineCitations,
    List<string> OptimizationRecommendations,
    DateTime AnalyzedAt,
    bool IsCached = false
);

public interface IGeoEngineService
{
    Task<GeoAnalysisResultDto> AnalyzeBrandGeoVisibilityAsync(Guid websiteId, Guid tenantId, bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<GeoAnalysisResultDto> EvaluateCustomPromptAsync(Guid websiteId, Guid tenantId, string promptText, bool forceRefresh = false, CancellationToken cancellationToken = default);
}
