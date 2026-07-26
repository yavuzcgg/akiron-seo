namespace AkironSeo.Application.Common.Interfaces;

public record ExecutiveReportDto(
    Guid WebsiteId,
    string WebsiteName,
    string DomainUrl,
    int SeoAuditScore,
    int AiShareOfVoiceScore,
    int GoldOpportunitiesCount,
    int TrackedKeywordsCount,
    string HtmlReportDocument,
    DateTime GeneratedAt
);

public interface IReportExportService
{
    Task<ExecutiveReportDto> GenerateExecutiveReportAsync(
        Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
