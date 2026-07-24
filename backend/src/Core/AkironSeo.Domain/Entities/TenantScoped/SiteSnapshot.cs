using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class SiteSnapshot : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid SeoAuditId { get; set; }
    public Guid WebsiteId { get; set; }
    public int Score { get; set; } = 100;
    public int TotalPagesCount { get; set; } = 0;
    public int TotalIssuesCount { get; set; } = 0;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public SeoAudit SeoAudit { get; set; } = null!;
    public Website Website { get; set; } = null!;
}
