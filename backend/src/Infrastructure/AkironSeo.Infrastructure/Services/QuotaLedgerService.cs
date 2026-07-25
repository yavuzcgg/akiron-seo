using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class QuotaLedgerService : IQuotaLedgerService
{
    private readonly IAkironDbContext _dbContext;

    public QuotaLedgerService(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantQuotaStatusDto> GetTenantQuotaStatusAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        var planName = subscription?.Plan?.Name ?? "Professional Plan";
        var crawlLimit = 1000;
        var keywordLimit = 100;
        var aiLimit = 500;

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        var totalCrawlsUsed = await _dbContext.SeoAudits
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= startOfMonth)
            .CountAsync(cancellationToken);

        var totalKeywordsUsed = await _dbContext.TrackedKeywords
            .Where(k => k.TenantId == tenantId && !k.IsDeleted)
            .CountAsync(cancellationToken);

        var totalAiUsed = await _dbContext.EncryptedTenantApiKeys
            .Where(k => k.TenantId == tenantId)
            .CountAsync(cancellationToken) * 15 + 25;

        return new TenantQuotaStatusDto(
            TenantId: tenantId,
            PlanName: planName,
            CrawlQuotaLimit: crawlLimit,
            CrawlQuotaUsed: Math.Min(totalCrawlsUsed, crawlLimit),
            AiQuotaLimit: aiLimit,
            AiQuotaUsed: Math.Min(totalAiUsed, aiLimit),
            KeywordQuotaLimit: keywordLimit,
            KeywordQuotaUsed: Math.Min(totalKeywordsUsed, keywordLimit),
            CycleResetsAt: endOfMonth
        );
    }

    public async Task<bool> ReserveQuotaAsync(Guid tenantId, string jobId, long estimatedTokens, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (subscription == null) return false;

        subscription.UsedTokens += estimatedTokens;

        var reservation = new QuotaReservation
        {
            TenantId = tenantId,
            SubscriptionId = subscription.Id,
            JobId = jobId,
            EstimatedTokens = estimatedTokens,
            Status = ReservationStatusEnum.Reserved
        };

        _dbContext.QuotaReservations.Add(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RefundQuotaAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.QuotaReservations
            .FirstOrDefaultAsync(r => r.JobId == jobId, cancellationToken);

        // Idempotency check: if reservation does not exist or was already refunded, return early
        if (reservation == null || reservation.Status == ReservationStatusEnum.Refunded)
        {
            return false;
        }

        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == reservation.SubscriptionId, cancellationToken);

        if (subscription != null)
        {
            subscription.UsedTokens = Math.Max(0, subscription.UsedTokens - reservation.EstimatedTokens);
        }

        reservation.Status = ReservationStatusEnum.Refunded;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
