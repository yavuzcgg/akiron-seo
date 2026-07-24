using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class ApiUsageLog : BaseEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string? JobId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public long TokensUsed { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Tenant Tenant { get; set; } = null!;
}
