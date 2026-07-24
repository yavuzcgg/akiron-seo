using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class SeoAudit : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid CrawlJobId { get; set; }
    public Guid WebsiteId { get; set; }
    public int OverallScore { get; set; } = 100;
    public string RobotsTxtAiStatusJson { get; set; } = "{}";

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public CrawlJob CrawlJob { get; set; } = null!;
    public Website Website { get; set; } = null!;
    public SiteSnapshot? SiteSnapshot { get; set; }
}
