namespace AkironSeo.Application.Common.Interfaces;

public interface IQuotaLedgerService
{
    Task<bool> ReserveQuotaAsync(Guid tenantId, string jobId, long estimatedTokens, CancellationToken cancellationToken = default);
    Task CommitQuotaAsync(string jobId, long actualTokens, CancellationToken cancellationToken = default);
    Task RefundQuotaAsync(string jobId, CancellationToken cancellationToken = default);
}
