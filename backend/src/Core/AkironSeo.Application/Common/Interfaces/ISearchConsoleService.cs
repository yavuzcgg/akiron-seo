using AkironSeo.Application.Common;

namespace AkironSeo.Application.Common.Interfaces;

public record GscMetricsDto(
    Guid WebsiteId,
    string DomainUrl,
    long TotalClicks,
    long TotalImpressions,
    double AverageCtrPercentage,
    double AveragePosition,
    int TopKeywordsCount,
    DateTime AnalyzedAt,
    // No Google Search Console integration exists yet — see DataSources.
    string DataSource = DataSources.Simulated
);

public interface ISearchConsoleService
{
    Task<GscMetricsDto> GetSearchConsoleAnalyticsAsync(
        Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
