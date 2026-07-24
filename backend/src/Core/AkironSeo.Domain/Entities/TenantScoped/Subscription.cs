using AkironSeo.Domain.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class Subscription : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatusEnum Status { get; set; } = SubscriptionStatusEnum.Active;
    public long MonthlyLimitTokens { get; set; } = 100000;
    public long UsedTokens { get; set; } = 0;
    public DateTime CurrentPeriodStart { get; set; } = DateTime.UtcNow;
    public DateTime CurrentPeriodEnd { get; set; } = DateTime.UtcNow.AddMonths(1);

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}
