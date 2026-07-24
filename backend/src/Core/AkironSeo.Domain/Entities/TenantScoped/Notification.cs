using AkironSeo.Domain.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class Notification : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public NotificationTypeEnum Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public User? User { get; set; }
}
