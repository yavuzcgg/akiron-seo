namespace AkironSeo.Application.Common.Interfaces;

public record TenantQuotaStatusDto(
    Guid TenantId,
    string PlanName,
    int CrawlQuotaLimit,
    int CrawlQuotaUsed,
    int AiQuotaLimit,
    int AiQuotaUsed,
    int KeywordQuotaLimit,
    int KeywordQuotaUsed,
    DateTime CycleResetsAt
);

public interface IQuotaLedgerService
{
    Task<TenantQuotaStatusDto> GetTenantQuotaStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ReserveQuotaAsync(Guid tenantId, string jobId, long estimatedTokens, CancellationToken cancellationToken = default);
    Task<bool> RefundQuotaAsync(string jobId, CancellationToken cancellationToken = default);
}
