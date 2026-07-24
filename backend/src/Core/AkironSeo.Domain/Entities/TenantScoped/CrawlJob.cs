using AkironSeo.Domain.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class CrawlJob : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid WebsiteId { get; set; }
    public CrawlStatusEnum Status { get; set; } = CrawlStatusEnum.Pending;
    public int PagesDiscovered { get; set; } = 0;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public Website Website { get; set; } = null!;
    public ICollection<CrawlResult> CrawlResults { get; set; } = new List<CrawlResult>();
    public SeoAudit? SeoAudit { get; set; }
}
