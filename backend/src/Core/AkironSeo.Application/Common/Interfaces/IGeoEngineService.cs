using AkironSeo.Application.Common;

namespace AkironSeo.Application.Common.Interfaces;

public record AiEngineCitationDto(
    string EngineName, // "Perplexity", "Gemini"
    bool IsMentioned,
    string Sentiment, // "Positive", "Neutral", "NotMentioned", "Unknown"
    string CitationUrl,
    string SampleAiResponseSnippet,
    int MentionRatePercentage = 100,
    string CitationStatus = "Valid",
    bool IsGoldOpportunity = false,
    // See DataSources. Anything other than Live means this row is not a measurement.
    string DataSource = DataSources.Live
);

public record GeoAnalysisResultDto(
    Guid WebsiteId,
    string DomainUrl,
    int ShareOfVoiceScore, // 0 - 100%
    int OverallMentionRatePercentage,
    List<AiEngineCitationDto> EngineCitations,
    List<string> OptimizationRecommendations,
    DateTime AnalyzedAt,
    bool IsCached = false,
    // Number of engines that actually answered. The scores above are computed only over
    // these, so an unconfigured engine lowers confidence rather than the score.
    int LiveEngineCount = 0
);

public interface IGeoEngineService
{
    Task<GeoAnalysisResultDto> AnalyzeBrandGeoVisibilityAsync(Guid websiteId, Guid tenantId, bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<GeoAnalysisResultDto> EvaluateCustomPromptAsync(Guid websiteId, Guid tenantId, string promptText, bool forceRefresh = false, CancellationToken cancellationToken = default);
}
