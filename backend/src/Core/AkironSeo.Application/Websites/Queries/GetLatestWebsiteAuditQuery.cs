using System.Text.Json;
using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Websites.Queries;

public record SeoIssueDto(string Code, string Severity, string Description, string Recommendation);

public record SeoAuditReportDto(
    Guid AuditId,
    Guid WebsiteId,
    string WebsiteName,
    string DomainUrl,
    int OverallScore,
    int StatusCode,
    string Title,
    string MetaDescription,
    string CanonicalUrl,
    List<string> H1Tags,
    Dictionary<string, string> OpenGraphTags,
    string RobotsMeta,
    List<SeoIssueDto> Issues,
    RobotsTxtAuditDto? RobotsTxtAudit,
    DateTime CrawledAt
);

public record GetLatestWebsiteAuditQuery(Guid WebsiteId) : IRequest<SeoAuditReportDto?>;

public class GetLatestWebsiteAuditQueryHandler : IRequestHandler<GetLatestWebsiteAuditQuery, SeoAuditReportDto?>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetLatestWebsiteAuditQueryHandler(IAkironDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<SeoAuditReportDto?> Handle(GetLatestWebsiteAuditQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;

        var audit = await _dbContext.SeoAudits
            .Where(a => a.WebsiteId == request.WebsiteId && a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (audit == null) return null;

        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == request.WebsiteId && w.TenantId == tenantId, cancellationToken);

        var crawlResult = await _dbContext.CrawlResults
            .Where(cr => cr.CrawlJobId == audit.CrawlJobId && cr.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        var title = crawlResult?.Title ?? website?.Name ?? "N/A";
        var metaDesc = crawlResult?.MetaDescription ?? "No meta description found.";
        var statusCode = crawlResult?.StatusCode ?? 200;
        var canonicalUrl = crawlResult?.CanonicalUrl ?? string.Empty;

        // Deserialize stored H1 tags
        var h1Tags = new List<string>();
        if (!string.IsNullOrEmpty(crawlResult?.H1Json) && crawlResult.H1Json != "[]")
        {
            try
            {
                h1Tags = JsonSerializer.Deserialize<List<string>>(crawlResult.H1Json) ?? new List<string>();
            }
            catch { /* Fallback to empty list */ }
        }

        // Deserialize stored issues from CrawlResult
        var issues = new List<SeoIssueDto>();
        if (!string.IsNullOrEmpty(crawlResult?.IssuesJson) && crawlResult.IssuesJson != "[]")
        {
            try
            {
                var rawIssues = JsonSerializer.Deserialize<List<JsonElement>>(crawlResult.IssuesJson);
                if (rawIssues != null)
                {
                    foreach (var item in rawIssues)
                    {
                        issues.Add(new SeoIssueDto(
                            Code: item.TryGetProperty("Code", out var c) ? c.GetString() ?? "" : "",
                            Severity: item.TryGetProperty("Severity", out var s) ? s.GetString() ?? "Info" : "Info",
                            Description: item.TryGetProperty("Description", out var d) ? d.GetString() ?? "" : "",
                            Recommendation: item.TryGetProperty("Recommendation", out var r) ? r.GetString() ?? "" : ""
                        ));
                    }
                }
            }
            catch { /* Fallback: regenerate issues inline */ }
        }

        // If no stored issues (legacy crawl data), generate them inline
        if (issues.Count == 0)
        {
            issues = GenerateLegacyIssues(title, metaDesc);
        }

        // Deserialize persisted robots.txt audit
        RobotsTxtAuditDto? robotsTxtAudit = null;
        if (!string.IsNullOrEmpty(audit.RobotsTxtAiStatusJson) && audit.RobotsTxtAiStatusJson != "{}")
        {
            try
            {
                robotsTxtAudit = JsonSerializer.Deserialize<RobotsTxtAuditDto>(
                    audit.RobotsTxtAiStatusJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { /* Fallback to null */ }
        }

        return new SeoAuditReportDto(
            AuditId: audit.Id,
            WebsiteId: request.WebsiteId,
            WebsiteName: website?.Name ?? "Website",
            DomainUrl: website?.DomainUrl ?? "",
            OverallScore: audit.OverallScore,
            StatusCode: statusCode,
            Title: title,
            MetaDescription: metaDesc,
            CanonicalUrl: canonicalUrl,
            H1Tags: h1Tags,
            OpenGraphTags: new Dictionary<string, string>(),
            RobotsMeta: string.Empty,
            Issues: issues,
            RobotsTxtAudit: robotsTxtAudit,
            CrawledAt: audit.CreatedAt
        );
    }

    /// <summary>
    /// Backward-compatible issue generation for crawl results that predate the enhanced crawler.
    /// </summary>
    private static List<SeoIssueDto> GenerateLegacyIssues(string title, string metaDesc)
    {
        var issues = new List<SeoIssueDto>();

        if (title.Length < 20)
        {
            issues.Add(new SeoIssueDto(
                Code: "TITLE_TOO_SHORT",
                Severity: "Warning",
                Description: $"Title tag '{title}' is too short ({title.Length} characters).",
                Recommendation: "Expand title tag to 30-60 characters including your main target keyword."
            ));
        }

        if (metaDesc.Length < 50)
        {
            issues.Add(new SeoIssueDto(
                Code: "META_DESC_TOO_SHORT",
                Severity: "Warning",
                Description: $"Meta description is too short ({metaDesc.Length} characters).",
                Recommendation: "Write a compelling meta description between 120-160 characters for maximum search CTR."
            ));
        }

        issues.Add(new SeoIssueDto(
            Code: "OPENGRAPH_MISSING",
            Severity: "Info",
            Description: "Social media OpenGraph tags (og:image, og:description) not configured.",
            Recommendation: "Add OpenGraph meta tags so your website previews look rich when shared on LinkedIn, Twitter, and WhatsApp."
        ));

        return issues;
    }
}
