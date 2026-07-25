using AkironSeo.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class CompetitorService : ICompetitorService
{
    private readonly IAkironDbContext _dbContext;

    public CompetitorService(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompetitorGapResultDto> AnalyzeCompetitorGapAsync(Guid websiteId, Guid tenantId, string competitorDomain, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new InvalidOperationException("Website not found.");
        }

        var yourDomain = website.DomainUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "").Split('/')[0];
        var cleanCompetitor = competitorDomain.Replace("https://", "").Replace("http://", "").Replace("www.", "").Split('/')[0];

        // Simulated SERP keyword gap analysis algorithm
        var hash = Math.Abs((yourDomain + cleanCompetitor).GetHashCode());
        var overlapScore = (hash % 40) + 45; // Overlap score between 45% and 85%

        var missingKeywords = new List<KeywordOpportunityDto>
        {
            new("motosiklet kask çeşitleri", 2, 14, 18500, "Orta"),
            new("toptan oto ve moto yedek parça", 1, 0, 12400, "Yüksek"),
            new("pamukova motor servisi ve bakım", 3, 22, 5400, "Düşük"),
            new("orijinal motosiklet ekipmanları", 4, 18, 9800, "Orta")
        };

        return new CompetitorGapResultDto(
            WebsiteId: websiteId,
            YourDomain: yourDomain,
            CompetitorDomain: cleanCompetitor,
            OverlapScore: overlapScore,
            MissingKeywordOpportunities: missingKeywords,
            AnalyzedAt: DateTime.UtcNow
        );
    }

    public async Task<List<CompetitorGapResultDto>> GetWebsiteCompetitorsAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        var domain = website?.DomainUrl ?? "b2bmotoyildirim.com";

        return new List<CompetitorGapResultDto>
        {
            await AnalyzeCompetitorGapAsync(websiteId, tenantId, "yamaha-motor.com.tr", cancellationToken)
        };
    }
}
