using AkironSeo.Domain.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class EncryptedTenantApiKey : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public AiProviderEnum Provider { get; set; }
    public string EncryptedKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation Property
    public Tenant Tenant { get; set; } = null!;
}
