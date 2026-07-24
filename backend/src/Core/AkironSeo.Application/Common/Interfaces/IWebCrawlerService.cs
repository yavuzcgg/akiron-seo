using AkironSeo.Domain.Entities.TenantScoped;

namespace AkironSeo.Application.Common.Interfaces;

public interface IWebCrawlerService
{
    Task<SeoAudit> CrawlAndAuditWebsiteAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default);
}
