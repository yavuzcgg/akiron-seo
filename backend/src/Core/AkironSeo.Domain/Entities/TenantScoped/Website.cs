using AkironSeo.Domain.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class Website : BaseEntity, IMultiTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string DomainUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = Guid.NewGuid().ToString("N");
    public VerificationMethodEnum VerificationMethod { get; set; } = VerificationMethodEnum.DnsTxt;
    public bool IsVerified { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    public string BrandAliasesJson { get; set; } = "[]";

    // Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<TrackedKeyword> TrackedKeywords { get; set; } = new List<TrackedKeyword>();
    public ICollection<CrawlJob> CrawlJobs { get; set; } = new List<CrawlJob>();
    public ICollection<SeoAudit> SeoAudits { get; set; } = new List<SeoAudit>();
    public ICollection<SiteSnapshot> SiteSnapshots { get; set; } = new List<SiteSnapshot>();
    public ICollection<AeoSchema> AeoSchemas { get; set; } = new List<AeoSchema>();
    public ICollection<AiContentPlan> AiContentPlans { get; set; } = new List<AiContentPlan>();
}
