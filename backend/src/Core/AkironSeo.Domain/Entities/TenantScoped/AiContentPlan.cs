using AkironSeo.Domain.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class AiContentPlan : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid WebsiteId { get; set; }
    public string TargetKeyword { get; set; } = string.Empty;
    public string GeneratedMarkdownContent { get; set; } = string.Empty;
    public ContentStatusEnum Status { get; set; } = ContentStatusEnum.Draft;
    public long TokensSpent { get; set; } = 0;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public Website Website { get; set; } = null!;
}
