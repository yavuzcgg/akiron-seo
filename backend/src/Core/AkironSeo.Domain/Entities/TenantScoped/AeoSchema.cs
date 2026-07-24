using AkironSeo.Domain.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class AeoSchema : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid WebsiteId { get; set; }
    public string PageUrl { get; set; } = string.Empty;
    public SchemaTypeEnum SchemaType { get; set; } = SchemaTypeEnum.Faq;
    public string JsonLdOutput { get; set; } = string.Empty;
    public string LlmsTxtOutput { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public Website Website { get; set; } = null!;
}
