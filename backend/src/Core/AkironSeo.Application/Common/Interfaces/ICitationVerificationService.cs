using AkironSeo.Domain.Enums;

namespace AkironSeo.Application.Common.Interfaces;

public record CitationVerificationResult(
    string TargetUrl,
    CitationStatusEnum Status,
    int HttpStatusCode,
    bool IsGoldOpportunity,
    string? MissingPath
);

public interface ICitationVerificationService
{
    Task<CitationVerificationResult> VerifyCitationUrlAsync(
        string url,
        string tenantDomain,
        Guid websiteId,
        Guid tenantId,
        string keyword,
        string engineName,
        CancellationToken cancellationToken = default);
}
