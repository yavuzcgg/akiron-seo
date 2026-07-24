using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class WebCrawlerService : IWebCrawlerService
{
    private readonly IAkironDbContext _dbContext;
    private readonly HttpClient _httpClient;

    public WebCrawlerService(IAkironDbContext dbContext, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }

    public async Task<SeoAudit> CrawlAndAuditWebsiteAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new KeyNotFoundException("Website not found.");
        }

        // 1. Create CrawlJob
        var crawlJob = new CrawlJob
        {
            TenantId = tenantId,
            WebsiteId = websiteId,
            Status = CrawlStatusEnum.Running,
            StartedAt = DateTime.UtcNow
        };
        _dbContext.CrawlJobs.Add(crawlJob);
        await _dbContext.SaveChangesAsync(cancellationToken);

        int totalPages = 0;

        try
        {
            // 2. Perform HTTP Fetch
            var targetUrl = $"https://{website.DomainUrl}";
            var response = await _httpClient.GetAsync(targetUrl, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Extract Title & Meta
            var title = ExtractTag(html, "<title>", "</title>") ?? website.Name;
            var metaDesc = ExtractMetaDescription(html) ?? "No meta description configured.";

            totalPages = 1;

            // 3. Create CrawlResult
            var crawlResult = new CrawlResult
            {
                TenantId = tenantId,
                CrawlJobId = crawlJob.Id,
                PageUrl = targetUrl,
                StatusCode = (int)response.StatusCode,
                Title = title,
                MetaDescription = metaDesc,
                IssuesJson = "[]"
            };
            _dbContext.CrawlResults.Add(crawlResult);

            // Complete CrawlJob
            crawlJob.Status = CrawlStatusEnum.Completed;
            crawlJob.CompletedAt = DateTime.UtcNow;
            crawlJob.PagesDiscovered = totalPages;

            // 4. Create SeoAudit linked 1-to-1 to CrawlJob
            int overallScore = response.IsSuccessStatusCode ? 92 : 45;
            var seoAudit = new SeoAudit
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                CrawlJobId = crawlJob.Id,
                OverallScore = overallScore,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.SeoAudits.Add(seoAudit);

            // 5. Create SiteSnapshot summary
            var snapshot = new SiteSnapshot
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                SeoAuditId = seoAudit.Id,
                TotalPagesCount = totalPages,
                TotalIssuesCount = 0,
                Score = overallScore
            };
            _dbContext.SiteSnapshots.Add(snapshot);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return seoAudit;
        }
        catch
        {
            crawlJob.Status = CrawlStatusEnum.Failed;
            crawlJob.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static string? ExtractTag(string html, string startTag, string endTag)
    {
        int startIndex = html.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return null;
        startIndex += startTag.Length;

        int endIndex = html.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex == -1) return null;

        return html.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static string? ExtractMetaDescription(string html)
    {
        int metaIndex = html.IndexOf("name=\"description\"", StringComparison.OrdinalIgnoreCase);
        if (metaIndex == -1) metaIndex = html.IndexOf("name='description'", StringComparison.OrdinalIgnoreCase);
        if (metaIndex == -1) return null;

        int contentIndex = html.IndexOf("content=\"", metaIndex, StringComparison.OrdinalIgnoreCase);
        if (contentIndex == -1) return null;
        contentIndex += 9;

        int endIndex = html.IndexOf("\"", contentIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex == -1) return null;

        return html.Substring(contentIndex, endIndex - contentIndex).Trim();
    }
}
