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
    List<SeoIssueDto> Issues,
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

        // Generate Detailed Issues & Actionable Fixes
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

        return new SeoAuditReportDto(
            AuditId: audit.Id,
            WebsiteId: request.WebsiteId,
            WebsiteName: website?.Name ?? "Website",
            DomainUrl: website?.DomainUrl ?? "",
            OverallScore: audit.OverallScore,
            StatusCode: statusCode,
            Title: title,
            MetaDescription: metaDesc,
            Issues: issues,
            CrawledAt: audit.CreatedAt
        );
    }
}
