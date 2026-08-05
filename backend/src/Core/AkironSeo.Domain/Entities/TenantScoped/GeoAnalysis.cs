using AkironSeo.Domain.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class GeoAnalysis : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }

    /// <summary>
    /// The website this analysis describes. Required: without it a multi-website tenant
    /// cannot tell whose results these are, and the 24-hour cache lookup returns another
    /// site's data.
    /// </summary>
    public Guid WebsiteId { get; set; }

    /// <summary>
    /// Optional: brand-visibility analysis runs against a prompt, which need not be tied
    /// to a tracked keyword. Previously non-nullable, which made every run for a website
    /// without keywords fail the foreign key.
    /// </summary>
    public Guid? TrackedKeywordId { get; set; }

    public Guid RunGroupId { get; set; }
    public TargetLlmEnum TargetEngine { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public Guid? PromptTemplateId { get; set; }
    public bool IsMentioned { get; set; } = false;
    public int? Position { get; set; }
    public MentionTypeEnum MentionType { get; set; } = MentionTypeEnum.Text;
    public CitationStatusEnum CitationStatus { get; set; } = CitationStatusEnum.Valid;
    public string CitationUrl { get; set; } = string.Empty;
    public string CompetitorsJson { get; set; } = "[]";
    public string RawResponseJson { get; set; } = "{}";

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public Website Website { get; set; } = null!;
    public TrackedKeyword? TrackedKeyword { get; set; }
    public PromptTemplate? PromptTemplate { get; set; }
}
