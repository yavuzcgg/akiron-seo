using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class CrawlResult : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid CrawlJobId { get; set; }
    public string PageUrl { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public string Title { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string H1Json { get; set; } = "[]";
    public string CanonicalUrl { get; set; } = string.Empty;
    public string IssuesJson { get; set; } = "[]";

    /// <summary>
    /// Per-component score contributions captured at crawl time. Some inputs to the score
    /// (OpenGraph tags, robots meta) are not persisted on this row, so the breakdown cannot
    /// be recomputed afterwards — it has to be stored when it is calculated.
    /// </summary>
    public string ScoreBreakdownJson { get; set; } = "[]";

    public string? PageSpeedMetricsJson { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public CrawlJob CrawlJob { get; set; } = null!;
}
