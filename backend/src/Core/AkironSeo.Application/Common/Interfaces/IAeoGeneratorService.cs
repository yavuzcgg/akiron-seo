namespace AkironSeo.Application.Common.Interfaces;

public record AeoSchemasDto(
    string OrganizationJsonLd,
    string WebSiteJsonLd,
    string FaqJsonLd,
    string LlmsTxtContent,
    string LlmsFullTxtContent
);

public interface IAeoGeneratorService
{
    Task<AeoSchemasDto> GenerateAeoSchemasAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
