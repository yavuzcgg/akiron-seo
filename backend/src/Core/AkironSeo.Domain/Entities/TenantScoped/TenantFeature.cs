using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class TenantFeature : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    // Navigation Property
    public Tenant Tenant { get; set; } = null!;
}
