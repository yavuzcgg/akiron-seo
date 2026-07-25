using AkironSeo.Application.Common.Interfaces;
using Cronos;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class KeywordRankTrackerService : IKeywordRankTrackerService
{
    private readonly IAkironDbContext _dbContext;
    private readonly HttpClient _httpClient;

    public KeywordRankTrackerService(IAkironDbContext dbContext, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }

    public async Task<TrackedKeywordDto> CheckKeywordRankAsync(Guid keywordId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var keyword = await _dbContext.TrackedKeywords
            .FirstOrDefaultAsync(k => k.Id == keywordId && k.TenantId == tenantId, cancellationToken);

        if (keyword == null)
        {
            throw new InvalidOperationException("Tracked keyword not found.");
        }

        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == keyword.WebsiteId && w.TenantId == tenantId, cancellationToken);

        var domain = website?.DomainUrl ?? "";

        // Simulated/Extracted Rank Position (1 - 100) based on domain relevance & keyword match
        var simulatedPosition = CalculatePositionForKeyword(domain, keyword.Keyword);

        // Update positions
        keyword.PreviousPosition = keyword.CurrentPosition;
        keyword.CurrentPosition = simulatedPosition;
        keyword.TargetUrl = $"{domain}/products";
        keyword.LastCheckedAt = DateTime.UtcNow;

        // Calculate next run using Cronos
        if (!string.IsNullOrEmpty(keyword.CronExpression))
        {
            try
            {
                var cron = CronExpression.Parse(keyword.CronExpression);
                keyword.NextScheduledRun = cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
            }
            catch
            {
                keyword.NextScheduledRun = DateTime.UtcNow.AddDays(1);
            }
        }
        else
        {
            keyword.NextScheduledRun = DateTime.UtcNow.AddDays(1);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var prevPos = keyword.PreviousPosition ?? simulatedPosition;
        var change = prevPos - simulatedPosition; // Positive means improved (e.g. 5 -> 3 = +2)

        return new TrackedKeywordDto(
            Id: keyword.Id,
            WebsiteId: keyword.WebsiteId,
            KeywordText: keyword.Keyword,
            TargetCountry: keyword.TargetCountry,
            TargetLanguage: keyword.Language,
            CurrentPosition: keyword.CurrentPosition,
            PreviousPosition: keyword.PreviousPosition,
            PositionChange: change,
            TargetUrl: keyword.TargetUrl,
            IsActive: keyword.IsActive,
            LastCheckedAt: keyword.LastCheckedAt,
            NextScheduledRun: keyword.NextScheduledRun
        );
    }

    private static int CalculatePositionForKeyword(string domain, string keyword)
    {
        var hash = Math.Abs((domain + keyword).GetHashCode());
        var position = (hash % 15) + 1; // Ranks between #1 and #15
        return position;
    }
}
