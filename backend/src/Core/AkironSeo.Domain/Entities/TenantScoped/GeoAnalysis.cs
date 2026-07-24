using AkironSeo.Domain.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class GeoAnalysis : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid TrackedKeywordId { get; set; }
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
    public TrackedKeyword TrackedKeyword { get; set; } = null!;
    public PromptTemplate? PromptTemplate { get; set; }
}
