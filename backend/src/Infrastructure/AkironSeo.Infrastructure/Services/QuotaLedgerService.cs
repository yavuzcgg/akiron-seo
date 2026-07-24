using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class QuotaLedgerService : IQuotaLedgerService
{
    private readonly AkironDbContext _dbContext;

    public QuotaLedgerService(AkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ReserveQuotaAsync(Guid tenantId, string jobId, long estimatedTokens, CancellationToken cancellationToken = default)
    {
        // 1. Check existing reservation idempotency
        var existingReservation = await _dbContext.QuotaReservations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.JobId == jobId, cancellationToken);

        if (existingReservation != null)
        {
            return existingReservation.Status == ReservationStatusEnum.Reserved;
        }

        // 2. Perform atomic conditional update on Subscriptions to prevent race condition
        var subscription = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatusEnum.Active, cancellationToken);

        if (subscription == null)
        {
            return false;
        }

        if (subscription.UsedTokens + estimatedTokens > subscription.MonthlyLimitTokens)
        {
            return false; // Quota Exceeded
        }

        subscription.UsedTokens += estimatedTokens;

        var reservation = new QuotaReservation
        {
            TenantId = tenantId,
            SubscriptionId = subscription.Id,
            JobId = jobId,
            EstimatedTokens = estimatedTokens,
            Status = ReservationStatusEnum.Reserved,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.QuotaReservations.Add(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task CommitQuotaAsync(string jobId, long actualTokens, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.QuotaReservations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.JobId == jobId, cancellationToken);

        if (reservation == null || reservation.Status != ReservationStatusEnum.Reserved)
        {
            return; // Already processed or non-existent
        }

        var subscription = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == reservation.SubscriptionId, cancellationToken);

        if (subscription != null)
        {
            long difference = actualTokens - reservation.EstimatedTokens;
            subscription.UsedTokens += difference;
        }

        reservation.ActualTokens = actualTokens;
        reservation.Status = ReservationStatusEnum.Committed;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RefundQuotaAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.QuotaReservations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.JobId == jobId, cancellationToken);

        if (reservation == null || reservation.Status != ReservationStatusEnum.Reserved)
        {
            return; // Idempotent refund: only refund if currently Reserved
        }

        var subscription = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == reservation.SubscriptionId, cancellationToken);

        if (subscription != null)
        {
            subscription.UsedTokens = Math.Max(0, subscription.UsedTokens - reservation.EstimatedTokens);
        }

        reservation.Status = ReservationStatusEnum.Refunded;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
