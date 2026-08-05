using AkironSeo.Application.Common;

namespace AkironSeo.Application.Common.Interfaces;

public record KeywordOpportunityDto(
    string Keyword,
    int CompetitorRank,
    int YourRank,
    int EstimatedSearchVolume,
    string Difficulty
);

public record CompetitorGapResultDto(
    Guid WebsiteId,
    string YourDomain,
    string CompetitorDomain,
    int OverlapScore, // 0 - 100%
    List<KeywordOpportunityDto> MissingKeywordOpportunities,
    DateTime AnalyzedAt,
    // No SERP provider is integrated yet, so the gap analysis is synthetic — see DataSources.
    string DataSource = DataSources.Simulated
);

public interface ICompetitorService
{
    Task<CompetitorGapResultDto> AnalyzeCompetitorGapAsync(Guid websiteId, Guid tenantId, string competitorDomain, CancellationToken cancellationToken = default);
    Task<List<CompetitorGapResultDto>> GetWebsiteCompetitorsAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
