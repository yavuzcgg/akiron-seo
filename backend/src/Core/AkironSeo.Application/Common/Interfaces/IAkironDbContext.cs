using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Common.Interfaces;

public interface IAkironDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Plan> Plans { get; }
    DbSet<PromptTemplate> PromptTemplates { get; }
    DbSet<AiCache> AiCaches { get; }
    DbSet<GlobalSystemLog> GlobalSystemLogs { get; }

    DbSet<Tenant> Tenants { get; }
    DbSet<TenantUser> TenantUsers { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<TenantFeature> TenantFeatures { get; }
    DbSet<EncryptedTenantApiKey> EncryptedTenantApiKeys { get; }
    DbSet<QuotaReservation> QuotaReservations { get; }
    DbSet<Website> Websites { get; }
    DbSet<TrackedKeyword> TrackedKeywords { get; }
    DbSet<CrawlJob> CrawlJobs { get; }
    DbSet<CrawlResult> CrawlResults { get; }
    DbSet<SeoAudit> SeoAudits { get; }
    DbSet<SiteSnapshot> SiteSnapshots { get; }
    DbSet<GeoAnalysis> GeoAnalyses { get; }
    DbSet<AeoSchema> AeoSchemas { get; }
    DbSet<AiContentPlan> AiContentPlans { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ApiUsageLog> ApiUsageLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
