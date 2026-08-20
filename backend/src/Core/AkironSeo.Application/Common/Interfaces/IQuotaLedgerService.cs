namespace AkironSeo.Application.Common.Interfaces;

public record TenantQuotaStatusDto(
    string PlanName,
    long MonthlyTokenLimit,
    long UsedTokens,
    long RemainingTokens,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool EnforcementEnabled
);

public interface IQuotaLedgerService
{
    Task<TenantQuotaStatusDto> GetTenantQuotaStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ReserveQuotaAsync(Guid tenantId, string jobId, long estimatedTokens, CancellationToken cancellationToken = default);
    Task<bool> CommitQuotaAsync(string jobId, long? actualTokens = null, CancellationToken cancellationToken = default);
    Task<bool> RefundQuotaAsync(string jobId, CancellationToken cancellationToken = default);
}
