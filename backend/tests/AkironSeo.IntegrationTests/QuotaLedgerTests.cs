using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AkironSeo.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class QuotaLedgerTests
{
    private readonly PostgresFixture _fixture;

    public QuotaLedgerTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReserveQuotaAsync_ShouldDeductTokensAndCreateReservation()
    {
        var tenantId = Guid.NewGuid();
        var jobId = NewJobId();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            var result = await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 200);

            Assert.True(result);
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var subscription = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
            var reservation = await dbContext.QuotaReservations.FirstAsync(r => r.JobId == jobId);

            Assert.Equal(200, subscription.UsedTokens);
            Assert.Equal(ReservationStatusEnum.Reserved, reservation.Status);
        }
    }

    [Fact]
    public async Task ReserveQuotaAsync_ShouldBeIdempotentForTheSameJobId()
    {
        var tenantId = Guid.NewGuid();
        var jobId = NewJobId();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        // A retried job (for example a re-delivered background message) must not debit twice.
        foreach (var _ in Enumerable.Range(0, 3))
        {
            await using var dbContext = _fixture.CreateDbContext(tenantId);
            var ledgerService = new QuotaLedgerService(dbContext);

            Assert.True(await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 200));
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var subscription = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
            var reservationCount = await dbContext.QuotaReservations.CountAsync(r => r.JobId == jobId);

            Assert.Equal(200, subscription.UsedTokens);
            Assert.Equal(1, reservationCount);
        }
    }

    [Fact]
    public async Task ReserveQuotaAsync_ShouldRejectReservationsExceedingTheMonthlyLimit()
    {
        var tenantId = Guid.NewGuid();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 500);

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);

            Assert.True(await ledgerService.ReserveQuotaAsync(tenantId, NewJobId(), estimatedTokens: 400));
            Assert.False(await ledgerService.ReserveQuotaAsync(tenantId, NewJobId(), estimatedTokens: 200));
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var subscription = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);

            // The rejected reservation must leave the balance untouched.
            Assert.Equal(400, subscription.UsedTokens);
        }
    }

    [Fact]
    public async Task ReserveQuotaAsync_ShouldNotOverdrawUnderConcurrentCallers()
    {
        var tenantId = Guid.NewGuid();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        // Ten concurrent reservations of 200 against a 1000 allowance: exactly five may succeed.
        var reservations = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var dbContext = _fixture.CreateDbContext(tenantId);
            var ledgerService = new QuotaLedgerService(dbContext);
            return await ledgerService.ReserveQuotaAsync(tenantId, NewJobId(), estimatedTokens: 200);
        });

        var results = await Task.WhenAll(reservations);

        Assert.Equal(5, results.Count(granted => granted));

        await using var assertContext = _fixture.CreateDbContext(tenantId);
        var subscription = await assertContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);

        Assert.Equal(1000, subscription.UsedTokens);
    }

    [Fact]
    public async Task RefundQuotaAsync_ShouldBeIdempotentAndPreventDoubleRefund()
    {
        var tenantId = Guid.NewGuid();
        var jobId = NewJobId();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 300);
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            Assert.True(await ledgerService.RefundQuotaAsync(jobId));
        }

        // Duplicate callback: the tokens are already back, so this must be a no-op.
        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            Assert.False(await ledgerService.RefundQuotaAsync(jobId));
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var subscription = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
            var reservation = await dbContext.QuotaReservations.FirstAsync(r => r.JobId == jobId);

            Assert.Equal(0, subscription.UsedTokens);
            Assert.Equal(ReservationStatusEnum.Refunded, reservation.Status);
        }
    }

    [Fact]
    public async Task RefundQuotaAsync_ShouldCreditTokensOnceUnderConcurrentCallers()
    {
        var tenantId = Guid.NewGuid();
        var jobId = NewJobId();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 300);
        }

        var refunds = Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var dbContext = _fixture.CreateDbContext(tenantId);
            var ledgerService = new QuotaLedgerService(dbContext);
            return await ledgerService.RefundQuotaAsync(jobId);
        });

        var results = await Task.WhenAll(refunds);

        Assert.Equal(1, results.Count(refunded => refunded));

        await using var assertContext = _fixture.CreateDbContext(tenantId);
        var subscription = await assertContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);

        Assert.Equal(0, subscription.UsedTokens);
    }

    [Fact]
    public async Task RefundQuotaAsync_ShouldSucceedWithoutAnAmbientTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var jobId = NewJobId();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 300);
        }

        // Background workers refund without ever resolving a tenant, which the global query
        // filter would otherwise turn into a silent no-op.
        await using (var dbContext = _fixture.CreateDbContext(Guid.Empty))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            Assert.True(await ledgerService.RefundQuotaAsync(jobId));
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var subscription = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);

            Assert.Equal(0, subscription.UsedTokens);
        }
    }

    [Fact]
    public async Task CommitQuotaAsync_ShouldTransitionStatusToCommittedAndAdjustTokens()
    {
        var tenantId = Guid.NewGuid();
        var jobId = NewJobId();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 300);
        }

        // Commit with actualTokens = 250 (50 fewer tokens than estimated 300)
        await using (var dbContext = _fixture.CreateDbContext(Guid.Empty))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            var committed = await ledgerService.CommitQuotaAsync(jobId, actualTokens: 250);
            Assert.True(committed);
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var reservation = await dbContext.QuotaReservations.FirstAsync(r => r.JobId == jobId);
            var subscription = await dbContext.Subscriptions.FirstAsync(s => s.TenantId == tenantId);

            Assert.Equal(ReservationStatusEnum.Committed, reservation.Status);
            Assert.Equal(250, subscription.UsedTokens);
        }
    }

    [Fact]
    public async Task CommitQuotaAsync_ShouldBeIdempotent()
    {
        var tenantId = Guid.NewGuid();
        var jobId = NewJobId();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 1000);

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var ledgerService = new QuotaLedgerService(dbContext);
            await ledgerService.ReserveQuotaAsync(tenantId, jobId, estimatedTokens: 200);
        }

        foreach (var _ in Enumerable.Range(0, 3))
        {
            await using var dbContext = _fixture.CreateDbContext(Guid.Empty);
            var ledgerService = new QuotaLedgerService(dbContext);
            Assert.True(await ledgerService.CommitQuotaAsync(jobId));
        }

        await using (var dbContext = _fixture.CreateDbContext(tenantId))
        {
            var reservation = await dbContext.QuotaReservations.FirstAsync(r => r.JobId == jobId);
            Assert.Equal(ReservationStatusEnum.Committed, reservation.Status);
        }
    }

    [Fact]
    public async Task GetTenantQuotaStatusAsync_ShouldReportEnforcementEnabled()
    {
        var tenantId = Guid.NewGuid();
        await SeedSubscriptionAsync(tenantId, monthlyLimitTokens: 500);

        await using var dbContext = _fixture.CreateDbContext(tenantId);
        var ledgerService = new QuotaLedgerService(dbContext);

        var status = await ledgerService.GetTenantQuotaStatusAsync(tenantId);
        Assert.True(status.EnforcementEnabled);
        Assert.Equal(500, status.MonthlyTokenLimit);
    }

    private static string NewJobId() => $"job-{Guid.NewGuid()}";

    private async Task SeedSubscriptionAsync(Guid tenantId, long monthlyLimitTokens)
    {
        await _fixture.SeedTenantAsync(tenantId);

        await using var dbContext = _fixture.CreateDbContext(tenantId);

        var plan = new Plan { Name = "Test Plan", PriceMonthly = 0m, LimitsJson = "{}" };
        dbContext.Plans.Add(plan);

        dbContext.Subscriptions.Add(new Subscription
        {
            TenantId = tenantId,
            PlanId = plan.Id,
            MonthlyLimitTokens = monthlyLimitTokens,
            UsedTokens = 0,
            Status = SubscriptionStatusEnum.Active
        });

        await dbContext.SaveChangesAsync();
    }
}
