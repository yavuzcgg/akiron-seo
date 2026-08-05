using AkironSeo.Application.Common;

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
    DateTime? NextScheduledRun,
    // No SERP provider is integrated yet, so positions are synthetic — see DataSources.
    string RankDataSource = DataSources.Simulated
);

public interface IKeywordRankTrackerService
{
    Task<TrackedKeywordDto> CheckKeywordRankAsync(Guid keywordId, Guid tenantId, CancellationToken cancellationToken = default);
}
