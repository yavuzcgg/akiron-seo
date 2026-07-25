namespace AkironSeo.Application.Common.Interfaces;

public record AeoSchemasDto(
    string OrganizationJsonLd,
    string WebSiteJsonLd,
    string LlmsTxtContent
);

public interface IAeoGeneratorService
{
    Task<AeoSchemasDto> GenerateAeoSchemasAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
