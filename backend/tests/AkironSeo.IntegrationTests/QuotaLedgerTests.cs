using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AkironSeo.IntegrationTests;

public class QuotaLedgerTests
{
    [Fact]
    public async Task ReserveQuotaAsync_ShouldDeductTokensAndCreateReservation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AkironDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantId = Guid.NewGuid();
        var jobId = "job-101";

        var tenantContextSeed = new TenantContext();
        tenantContextSeed.SetTenantId(tenantId);

        using (var dbContext = new AkironDbContext(options, tenantContextSeed))
        {
            dbContext.Subscriptions.Add(new Subscription
            {
                TenantId = tenantId,
                MonthlyLimitTokens = 1000,
                UsedTokens = 0,
                Status = SubscriptionStatusEnum.Active
            });
            await dbContext.SaveChangesAsync();
        }

        // Act
        var tenantContextAct = new TenantContext();
        tenantContextAct.SetTenantId(tenantId);
        using (var dbContext = new AkironDbContext(options, tenantContextAct))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            var result = await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 200);

            Assert.True(result);
        }

        // Assert
        var tenantContextAssert = new TenantContext();
        tenantContextAssert.SetTenantId(tenantId);
        using (var dbContext = new AkironDbContext(options, tenantContextAssert))
        {
            var sub = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
            var reservation = await dbContext.QuotaReservations.FirstAsync(r => r.JobId == jobId);

            Assert.Equal(200, sub.UsedTokens);
            Assert.Equal(ReservationStatusEnum.Reserved, reservation.Status);
        }
    }

    [Fact]
    public async Task RefundQuotaAsync_ShouldBeIdempotentAndPreventDoubleRefund()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AkironDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantId = Guid.NewGuid();
        var jobId = "job-102";

        var tenantContextSeed = new TenantContext();
        tenantContextSeed.SetTenantId(tenantId);

        using (var dbContext = new AkironDbContext(options, tenantContextSeed))
        {
            dbContext.Subscriptions.Add(new Subscription
            {
                TenantId = tenantId,
                MonthlyLimitTokens = 1000,
                UsedTokens = 0,
                Status = SubscriptionStatusEnum.Active
            });
            await dbContext.SaveChangesAsync();
        }

        var tenantContextReserve = new TenantContext();
        tenantContextReserve.SetTenantId(tenantId);
        using (var dbContext = new AkironDbContext(options, tenantContextReserve))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 300);
        }

        // Act 1: First Refund
        var tenantContextRefund1 = new TenantContext();
        tenantContextRefund1.SetTenantId(tenantId);
        using (var dbContext = new AkironDbContext(options, tenantContextRefund1))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.RefundQuotaAsync(jobId);
        }

        // Act 2: Second Attempted Refund (Simulating duplicate Hangfire retry / callback)
        var tenantContextRefund2 = new TenantContext();
        tenantContextRefund2.SetTenantId(tenantId);
        using (var dbContext = new AkironDbContext(options, tenantContextRefund2))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.RefundQuotaAsync(jobId);
        }

        // Assert: Tokens should only be refunded ONCE (UsedTokens = 0, not negative!)
        var tenantContextAssert = new TenantContext();
        tenantContextAssert.SetTenantId(tenantId);
        using (var dbContext = new AkironDbContext(options, tenantContextAssert))
        {
            var sub = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
            var reservation = await dbContext.QuotaReservations.FirstAsync(r => r.JobId == jobId);

            Assert.Equal(0, sub.UsedTokens);
            Assert.Equal(ReservationStatusEnum.Refunded, reservation.Status);
        }
    }
}
