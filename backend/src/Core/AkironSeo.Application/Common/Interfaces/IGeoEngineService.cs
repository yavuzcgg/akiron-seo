namespace AkironSeo.Application.Common.Interfaces;

public record AiEngineCitationDto(
    string EngineName, // "ChatGPT", "Perplexity", "Claude", "Gemini"
    bool IsMentioned,
    string Sentiment, // "Positive", "Neutral", "NotMentioned"
    string CitationUrl,
    string SampleAiResponseSnippet
);

public record GeoAnalysisResultDto(
    Guid WebsiteId,
    string DomainUrl,
    int ShareOfVoiceScore, // 0 - 100%
    List<AiEngineCitationDto> EngineCitations,
    List<string> OptimizationRecommendations,
    DateTime AnalyzedAt
);

public interface IGeoEngineService
{
    Task<GeoAnalysisResultDto> AnalyzeBrandGeoVisibilityAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<GeoAnalysisResultDto> EvaluateCustomPromptAsync(Guid websiteId, Guid tenantId, string promptText, CancellationToken cancellationToken = default);
}
