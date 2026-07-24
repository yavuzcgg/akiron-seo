using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class TrackedKeyword : BaseEntity, IMultiTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public Guid WebsiteId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string TargetEnginesJson { get; set; } = "[\"PerplexitySonar\", \"OpenAiSearch\"]";
    public string CronExpression { get; set; } = "0 0 * * *"; // Default daily at midnight
    public DateTime? NextScheduledRun { get; set; }
    public bool IsActive { get; set; } = true;

    // Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public Website Website { get; set; } = null!;
    public ICollection<GeoAnalysis> GeoAnalyses { get; set; } = new List<GeoAnalysis>();
}
