namespace AkironSeo.Application.Common.Interfaces;

public record TrackedKeywordDto(
    Guid Id,
    Guid WebsiteId,
    string KeywordText,
    string TargetCountry,
    string TargetLanguage,
    int? CurrentPosition,
    int? PreviousPosition,
    int PositionChange, // Positive = improved, Negative = dropped, 0 = unchanged
    string? TargetUrl,
    bool IsActive,
    DateTime? LastCheckedAt,
    DateTime? NextScheduledRun
);

public interface IKeywordRankTrackerService
{
    Task<TrackedKeywordDto> CheckKeywordRankAsync(Guid keywordId, Guid tenantId, CancellationToken cancellationToken = default);
}
