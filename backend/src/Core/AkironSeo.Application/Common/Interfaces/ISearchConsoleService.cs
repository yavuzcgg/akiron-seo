namespace AkironSeo.Application.Common.Interfaces;

public record GscMetricsDto(
    Guid WebsiteId,
    string DomainUrl,
    long TotalClicks,
    long TotalImpressions,
    double AverageCtrPercentage,
    double AveragePosition,
    int TopKeywordsCount,
    DateTime AnalyzedAt
);

public interface ISearchConsoleService
{
    Task<GscMetricsDto> GetSearchConsoleAnalyticsAsync(
        Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
