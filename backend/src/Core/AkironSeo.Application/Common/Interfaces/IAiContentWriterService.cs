using AkironSeo.Domain.Enums;

namespace AkironSeo.Application.Common.Interfaces;

public record AiContentPlanDto(
    Guid Id,
    Guid WebsiteId,
    string TargetKeyword,
    string? MissingPath,
    string GeneratedMarkdownContent,
    ContentStatusEnum Status,
    long TokensSpent,
    DateTime CreatedAt
);

public interface IAiContentWriterService
{
    Task<AiContentPlanDto> GenerateGeoContentAsync(
        Guid websiteId,
        Guid tenantId,
        string targetKeyword,
        string? missingPath = null,
        CancellationToken cancellationToken = default);
}
