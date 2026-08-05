using System.Net;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class CitationVerificationService : ICitationVerificationService
{
    private readonly IAkironDbContext _dbContext;
    private readonly INotificationDispatcherService _dispatcher;
    private readonly HttpClient _httpClient;

    public CitationVerificationService(
        IAkironDbContext dbContext,
        INotificationDispatcherService dispatcher,
        HttpClient httpClient)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _httpClient = httpClient;
    }

    public async Task<CitationVerificationResult> VerifyCitationUrlAsync(
        string url,
        string tenantDomain,
        Guid websiteId,
        Guid tenantId,
        string keyword,
        string engineName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new CitationVerificationResult(url, CitationStatusEnum.Unreachable, 0, false, null);
        }

        var cleanTargetUrl = NormalizeUrl(url);
        var cleanTenantDomain = NormalizeDomain(tenantDomain);

        Uri targetUri;
        try
        {
            targetUri = new Uri(cleanTargetUrl);
        }
        catch
        {
            return new CitationVerificationResult(url, CitationStatusEnum.Unreachable, 0, false, null);
        }

        // Check domain match
        var targetHost = NormalizeDomain(targetUri.Host);
        if (!targetHost.Equals(cleanTenantDomain, StringComparison.OrdinalIgnoreCase) &&
            !targetHost.EndsWith("." + cleanTenantDomain, StringComparison.OrdinalIgnoreCase))
        {
            return new CitationVerificationResult(cleanTargetUrl, CitationStatusEnum.WrongDomain, 200, false, null);
        }

        // URL belongs to tenant's domain: Verify via HTTP HEAD / GET
        int statusCode = 0;
        CitationStatusEnum status = CitationStatusEnum.Unreachable;
        bool isGoldOpportunity = false;
        string? missingPath = null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var request = new HttpRequestMessage(HttpMethod.Head, targetUri);
            request.Headers.Add("User-Agent", "AkironSeo-CitationVerifier/1.0");

            var response = await _httpClient.SendAsync(request, cts.Token);
            statusCode = (int)response.StatusCode;

            if (response.StatusCode == HttpStatusCode.MethodNotAllowed || response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Fallback to GET if HEAD is not allowed
                var getRequest = new HttpRequestMessage(HttpMethod.Get, targetUri);
                getRequest.Headers.Add("User-Agent", "AkironSeo-CitationVerifier/1.0");
                var getResponse = await _httpClient.SendAsync(getRequest, cts.Token);
                statusCode = (int)getResponse.StatusCode;
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                status = CitationStatusEnum.Valid;
            }
            else if (statusCode == 404)
            {
                status = CitationStatusEnum.NonExistentPage;
                isGoldOpportunity = true;
                missingPath = targetUri.AbsolutePath;
            }
            else
            {
                status = CitationStatusEnum.Unreachable;
            }
        }
        catch (TaskCanceledException)
        {
            status = CitationStatusEnum.Unreachable;
            statusCode = 408;
        }
        catch
        {
            status = CitationStatusEnum.Unreachable;
            statusCode = 500;
        }

        // If 404 Gold Opportunity, create notification record
        if (isGoldOpportunity)
        {
            await TriggerGoldOpportunityNotificationAsync(
                websiteId, tenantId, keyword, engineName, cleanTargetUrl, targetUri.AbsolutePath, cancellationToken);
        }

        return new CitationVerificationResult(cleanTargetUrl, status, statusCode, isGoldOpportunity, missingPath);
    }

    private async Task TriggerGoldOpportunityNotificationAsync(
        Guid websiteId,
        Guid tenantId,
        string keyword,
        string engineName,
        string fullUrl,
        string missingPath,
        CancellationToken cancellationToken)
    {
        // Avoid duplicate notification for the same missing path within 24 hours
        var existingNotification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n =>
                n.TenantId == tenantId &&
                n.WebsiteId == websiteId &&
                n.Type == NotificationTypeEnum.GoldOpportunityAlert &&
                n.Message.Contains(missingPath) &&
                n.CreatedAt > DateTime.UtcNow.AddDays(-1), cancellationToken);

        if (existingNotification != null) return;

        var notification = new Notification
        {
            TenantId = tenantId,
            WebsiteId = websiteId,
            Type = NotificationTypeEnum.GoldOpportunityAlert,
            Title = $"🌟 Gold GEO Opportunity: 404 Missing Page Cited by {engineName}",
            Message = $"{engineName} cited '{fullUrl}' for keyword '{keyword}', but this page returns 404. Create this page now using AI Content Writer to capture instant GEO traffic!",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId, cancellationToken);

        await _dispatcher.DispatchNotificationAlertAsync(new NotificationPayloadDto(
            EventType: "gold_opportunity_detected",
            WebsiteId: websiteId,
            WebsiteName: website?.Name ?? "Website",
            DomainUrl: website?.DomainUrl ?? "",
            Title: notification.Title,
            Message: notification.Message,
            Timestamp: notification.CreatedAt
        ), website?.WebhookUrl, cancellationToken);
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "https://" + trimmed;
        }
        return trimmed;
    }

    private static string NormalizeDomain(string domain)
    {
        return domain
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("www.", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/')
            .Trim();
    }
}
