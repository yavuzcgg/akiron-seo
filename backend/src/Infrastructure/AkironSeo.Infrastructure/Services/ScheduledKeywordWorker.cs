using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services;

public class ScheduledKeywordWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundJobQueue _jobQueue;
    private readonly ILogger<ScheduledKeywordWorker> _logger;
    private readonly TimeSpan _checkInterval;

    public ScheduledKeywordWorker(
        IServiceProvider serviceProvider,
        IBackgroundJobQueue jobQueue,
        ILogger<ScheduledKeywordWorker> logger,
        TimeSpan? checkInterval = null)
    {
        _serviceProvider = serviceProvider;
        _jobQueue = jobQueue;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledKeywordWorker background engine started with interval {Interval}s.", _checkInterval.TotalSeconds);

        // Run the queue consumer concurrently with the periodic scheduler
        var queueProcessingTask = ProcessBackgroundJobQueueAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueKeywordsAsync(stoppingToken);
                await ProcessSubscriptionExpirySweepAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during scheduled keyword processing cycle.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        await queueProcessingTask;
        _logger.LogInformation("ScheduledKeywordWorker background engine stopped.");
    }

    private async Task ProcessBackgroundJobQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _jobQueue.DequeueAsync(stoppingToken);
                using var scope = _serviceProvider.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing queued background job.");
            }
        }
    }

    public async Task<int> ProcessDueKeywordsAsync(CancellationToken stoppingToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAkironDbContext>();

        var now = DateTime.UtcNow;
        var dueKeywords = await dbContext.TrackedKeywords
            .IgnoreQueryFilters()
            .Where(k => k.IsActive && k.NextScheduledRun != null && k.NextScheduledRun <= now)
            .OrderBy(k => k.NextScheduledRun)
            .Take(50)
            .Select(k => new { k.Id, k.TenantId, k.Keyword })
            .ToListAsync(stoppingToken);

        if (dueKeywords.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation("Found {Count} due tracked keywords for scheduled rank checks.", dueKeywords.Count);

        int processed = 0;
        foreach (var item in dueKeywords)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                using var itemScope = _serviceProvider.CreateScope();
                var itemTenantContext = itemScope.ServiceProvider.GetRequiredService<ITenantContext>();
                itemTenantContext.SetTenantId(item.TenantId);

                var itemRankTracker = itemScope.ServiceProvider.GetRequiredService<IKeywordRankTrackerService>();

                await itemRankTracker.CheckKeywordRankAsync(item.Id, item.TenantId, stoppingToken);
                processed++;
                _logger.LogInformation("Successfully updated scheduled rank check for keyword '{Keyword}' ({Id}).", item.Keyword, item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute scheduled rank check for keyword '{Keyword}' ({Id}).", item.Keyword, item.Id);
            }
        }

        return processed;
    }

    public async Task<int> ProcessSubscriptionExpirySweepAsync(CancellationToken stoppingToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAkironDbContext>();

        var now = DateTime.UtcNow;
        var expiredSubscriptions = await dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatusEnum.Active && s.CurrentPeriodEnd < now)
            .ToListAsync(stoppingToken);

        if (expiredSubscriptions.Count == 0)
        {
            return 0;
        }

        foreach (var sub in expiredSubscriptions)
        {
            sub.Status = SubscriptionStatusEnum.PastDue;
            _logger.LogWarning("Subscription {SubId} for Tenant {TenantId} expired on {End} and transitioned to PastDue.", sub.Id, sub.TenantId, sub.CurrentPeriodEnd);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
        return expiredSubscriptions.Count;
    }
}
