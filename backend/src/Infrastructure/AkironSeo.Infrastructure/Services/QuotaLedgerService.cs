using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AkironSeo.Infrastructure.Services;

/// <summary>
/// Idempotent quota ledger. Every balance change is a single conditional UPDATE so that
/// concurrent callers cannot lose each other's writes, and each job id may be reserved once.
/// </summary>
public class QuotaLedgerService : IQuotaLedgerService
{
    private readonly AkironDbContext _dbContext;

    public QuotaLedgerService(AkironDbContext dbContext)
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

    /// <summary>
    /// Reserves <paramref name="estimatedTokens"/> against the tenant's monthly allowance.
    /// Safe to call repeatedly with the same <paramref name="jobId"/>: only the first call debits.
    /// Returns false when the reservation would exceed the allowance or the tenant has no subscription.
    /// </summary>
    public async Task<bool> ReserveQuotaAsync(Guid tenantId, string jobId, long estimatedTokens, CancellationToken cancellationToken = default)
    {
        // Query filters are bypassed throughout: background jobs carry no tenant context,
        // and the job id is globally unique anyway.
        var existingStatus = await _dbContext.QuotaReservations
            .IgnoreQueryFilters()
            .Where(r => r.JobId == jobId)
            .Select(r => (ReservationStatusEnum?)r.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingStatus is not null)
        {
            // Job ids are single-use. A live reservation makes this retry a no-op success;
            // a refunded one cannot be re-reserved, so the caller must not proceed.
            return existingStatus != ReservationStatusEnum.Refunded;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var subscriptionId = await _dbContext.Subscriptions
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscriptionId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Conditional debit. Zero rows means the allowance would have been exceeded;
            // the WHERE clause is what keeps concurrent reservations from overdrawing.
            var debitedRows = await _dbContext.Subscriptions
                .IgnoreQueryFilters()
                .Where(s => s.Id == subscriptionId && s.UsedTokens + estimatedTokens <= s.MonthlyLimitTokens)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(s => s.UsedTokens, s => s.UsedTokens + estimatedTokens),
                    cancellationToken);

            if (debitedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var entry = _dbContext.QuotaReservations.Add(new QuotaReservation
            {
                TenantId = tenantId,
                SubscriptionId = subscriptionId.Value,
                JobId = jobId,
                EstimatedTokens = estimatedTokens,
                Status = ReservationStatusEnum.Reserved
            });

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                entry.State = EntityState.Detached;
                await transaction.RollbackAsync(cancellationToken);

                // A concurrent caller inserted this job id first; their debit stands, so the
                // reservation this caller asked for does exist.
                if (exception is DbUpdateException updateException && IsUniqueViolation(updateException))
                {
                    return true;
                }

                throw;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    /// <summary>
    /// Returns a reservation's tokens to the tenant's allowance.
    /// Safe to call repeatedly: only the call that flips Reserved to Refunded credits the tokens back.
    /// </summary>
    public async Task<bool> RefundQuotaAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var reservation = await _dbContext.QuotaReservations
                .IgnoreQueryFilters()
                .Where(r => r.JobId == jobId)
                .Select(r => new { r.SubscriptionId, r.EstimatedTokens })
                .FirstOrDefaultAsync(cancellationToken);

            if (reservation is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var subscriptionId = reservation.SubscriptionId;
            var reservedTokens = reservation.EstimatedTokens;

            // Claim the reservation atomically. Concurrent refunds serialize on this row, so
            // exactly one of them observes Reserved and gets to credit the tokens back.
            var claimedRows = await _dbContext.QuotaReservations
                .IgnoreQueryFilters()
                .Where(r => r.JobId == jobId && r.Status == ReservationStatusEnum.Reserved)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(r => r.Status, ReservationStatusEnum.Refunded),
                    cancellationToken);

            if (claimedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await _dbContext.Subscriptions
                .IgnoreQueryFilters()
                .Where(s => s.Id == subscriptionId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        s => s.UsedTokens,
                        s => s.UsedTokens - reservedTokens < 0 ? 0 : s.UsedTokens - reservedTokens),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
