using AkironSeo.Application.Common;
using AkironSeo.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

/// <summary>
/// PLACEHOLDER. There is no Google Search Console integration yet — no OAuth flow and no
/// Google API client exist in this solution. The figures below are derived arithmetically
/// from the local audit score and keyword count purely so the dashboard has a shape to
/// render, and every result is tagged <see cref="DataSources.Simulated"/> so the UI can
/// label it. Replace this with a real Search Console query before treating any of it as
/// a measurement.
/// </summary>
public class SearchConsoleService : ISearchConsoleService
{
    private readonly IAkironDbContext _dbContext;

    public SearchConsoleService(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GscMetricsDto> GetSearchConsoleAnalyticsAsync(
        Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new InvalidOperationException("Website not found.");
        }

        var trackedKeywordsCount = await _dbContext.TrackedKeywords
            .CountAsync(k => k.WebsiteId == websiteId && k.TenantId == tenantId && k.IsActive, cancellationToken);

        var audit = await _dbContext.SeoAudits
            .Where(a => a.WebsiteId == websiteId && a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        int score = audit?.OverallScore ?? 85;

        // Synthetic figures — not organic search data. See the class summary.
        long baseImpressions = (long)(score * 350 + trackedKeywordsCount * 1250);
        long baseClicks = (long)(baseImpressions * 0.048);
        double ctr = baseImpressions > 0 ? (double)baseClicks / baseImpressions * 100 : 4.8;
        double avgPos = Math.Max(12.5 - (score / 10.0), 1.8);

        return new GscMetricsDto(
            WebsiteId: websiteId,
            DomainUrl: website.DomainUrl,
            TotalClicks: baseClicks,
            TotalImpressions: baseImpressions,
            AverageCtrPercentage: Math.Round(ctr, 2),
            AveragePosition: Math.Round(avgPos, 1),
            TopKeywordsCount: Math.Max(trackedKeywordsCount, 5),
            AnalyzedAt: DateTime.UtcNow,
            DataSource: DataSources.Simulated
        );
    }
}
