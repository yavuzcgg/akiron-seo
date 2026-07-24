using AkironSeo.Domain.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class TenantUser : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public UserRoleEnum Role { get; set; } = UserRoleEnum.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
}
