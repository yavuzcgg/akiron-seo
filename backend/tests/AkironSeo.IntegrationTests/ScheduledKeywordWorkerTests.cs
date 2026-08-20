using AkironSeo.Application.Common;
using AkironSeo.Application.Common.Exceptions;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;

namespace AkironSeo.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class ScheduledKeywordWorkerTests
{
    private readonly PostgresFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ScheduledKeywordWorkerTests(PostgresFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private class TestLogger<T> : ILogger<T>
    {
        private readonly ITestOutputHelper _output;
        public TestLogger(ITestOutputHelper output) => _output = output;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _output.WriteLine($"[{logLevel}] {formatter(state, exception)} {(exception != null ? exception.ToString() : "")}");
        }
    }

    [Fact]
    public async Task ScheduledKeywordWorker_ShouldProcessDueKeywords_AndUpdateNextScheduledRun()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        Guid websiteId;
        Guid dueKeywordId;
        Guid futureKeywordId;
        Guid inactiveKeywordId;

        var pastTime = DateTime.UtcNow.AddMinutes(-10);
        var futureTime = DateTime.UtcNow.AddHours(5);

        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var website = new Website
            {
                TenantId = tenantId,
                DomainUrl = "example.com",
                Name = "Example Site",
                VerificationToken = "token-123",
                IsVerified = true
            };
            db.Websites.Add(website);
            await db.SaveChangesAsync();
            websiteId = website.Id;

            var dueKeyword = new TrackedKeyword
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                Keyword = "seo agency",
                Language = "en",
                TargetCountry = "US",
                CronExpression = "0 * * * *", // hourly
                IsActive = true,
                NextScheduledRun = pastTime
            };
            var futureKeyword = new TrackedKeyword
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                Keyword = "keyword rank tracker",
                Language = "en",
                TargetCountry = "US",
                CronExpression = "0 * * * *",
                IsActive = true,
                NextScheduledRun = futureTime
            };
            var inactiveKeyword = new TrackedKeyword
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                Keyword = "inactive seo",
                Language = "en",
                TargetCountry = "US",
                CronExpression = "0 * * * *",
                IsActive = false,
                NextScheduledRun = pastTime
            };

            db.TrackedKeywords.AddRange(dueKeyword, futureKeyword, inactiveKeyword);
            await db.SaveChangesAsync();

            dueKeywordId = dueKeyword.Id;
            futureKeywordId = futureKeyword.Id;
            inactiveKeywordId = inactiveKeyword.Id;
        }

        // Setup DI for the worker test
        var services = new ServiceCollection();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<AkironDbContext>(options =>
        {
            options.UseNpgsql(_fixture.ConnectionString);
        });
        services.AddScoped<IAkironDbContext>(sp => sp.GetRequiredService<AkironDbContext>());
        services.AddScoped<IKeywordRankTrackerService, KeywordRankTrackerService>();
        services.AddHttpClient();
        var serviceProvider = services.BuildServiceProvider();

        var jobQueue = new BackgroundJobQueue();
        var worker = new ScheduledKeywordWorker(
            serviceProvider,
            jobQueue,
            new TestLogger<ScheduledKeywordWorker>(_output),
            TimeSpan.FromMilliseconds(50));

        var processed = await worker.ProcessDueKeywordsAsync(CancellationToken.None);
        Assert.Equal(1, processed);

        // Verify that the due keyword was updated
        await using (var assertDb = _fixture.CreateDbContext(tenantId))
        {
            var updatedDueKeyword = await assertDb.TrackedKeywords.FirstAsync(k => k.Id == dueKeywordId);
            var updatedFutureKeyword = await assertDb.TrackedKeywords.FirstAsync(k => k.Id == futureKeywordId);
            var updatedInactiveKeyword = await assertDb.TrackedKeywords.FirstAsync(k => k.Id == inactiveKeywordId);

            Assert.True(updatedDueKeyword.NextScheduledRun > DateTime.UtcNow);
            Assert.NotNull(updatedDueKeyword.LastCheckedAt);
            AssertEqualTimestamps(futureTime, updatedFutureKeyword.NextScheduledRun!.Value);
            AssertEqualTimestamps(pastTime, updatedInactiveKeyword.NextScheduledRun!.Value);
        }
    }

    private static void AssertEqualTimestamps(DateTime expected, DateTime actual)
    {
        Assert.True(Math.Abs((expected - actual).TotalMilliseconds) < 5, $"Expected {expected:O} but got {actual:O}");
    }

    [Fact]
    public async Task WebCrawlerService_ShouldRejectCrawl_WhenQuotaIsExceeded()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        Guid websiteId;
        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var plan = new Plan { Name = "Low Token Plan", PriceMonthly = 10m, LimitsJson = "{}" };
            db.Plans.Add(plan);
            db.Subscriptions.Add(new Subscription
            {
                TenantId = tenantId,
                PlanId = plan.Id,
                MonthlyLimitTokens = 2, // Less than QuotaCostConstants.CrawlCost (5)
                UsedTokens = 0,
                Status = SubscriptionStatusEnum.Active
            });

            var website = new Website
            {
                TenantId = tenantId,
                DomainUrl = "example.com",
                Name = "Example Site",
                VerificationToken = "tok-1",
                IsVerified = true
            };
            db.Websites.Add(website);
            await db.SaveChangesAsync();
            websiteId = website.Id;
        }

        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var ledger = new QuotaLedgerService(db);
            var crawler = new WebCrawlerService(db, new HttpClient(), new RobotsTxtAuditorService(new HttpClient()), ledger);

            await Assert.ThrowsAsync<QuotaExceededException>(() =>
                crawler.CrawlAndAuditWebsiteAsync(websiteId, tenantId));
        }

        // Verify that subscription used tokens remained 0
        await using (var assertDb = _fixture.CreateDbContext(tenantId))
        {
            var sub = await assertDb.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
            Assert.Equal(0, sub.UsedTokens);
        }
    }
}
