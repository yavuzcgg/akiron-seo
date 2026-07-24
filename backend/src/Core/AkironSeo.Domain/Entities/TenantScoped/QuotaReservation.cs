using AkironSeo.Domain.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class QuotaReservation : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public long EstimatedTokens { get; set; }
    public long? ActualTokens { get; set; }
    public ReservationStatusEnum Status { get; set; } = ReservationStatusEnum.Reserved;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public Subscription Subscription { get; set; } = null!;
}
