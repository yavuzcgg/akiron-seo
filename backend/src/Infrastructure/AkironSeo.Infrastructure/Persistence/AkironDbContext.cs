using System.Reflection;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Persistence;

public class AkironDbContext : DbContext, IAkironDbContext
{
    private readonly ITenantContext _tenantContext;

    public AkironDbContext(DbContextOptions<AkironDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Global Entities
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<AiCache> AiCaches => Set<AiCache>();
    public DbSet<GlobalSystemLog> GlobalSystemLogs => Set<GlobalSystemLog>();

    // Tenant-Scoped Entities
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();
    public DbSet<EncryptedTenantApiKey> EncryptedTenantApiKeys => Set<EncryptedTenantApiKey>();
    public DbSet<QuotaReservation> QuotaReservations => Set<QuotaReservation>();
    public DbSet<Website> Websites => Set<Website>();
    public DbSet<TrackedKeyword> TrackedKeywords => Set<TrackedKeyword>();
    public DbSet<CrawlJob> CrawlJobs => Set<CrawlJob>();
    public DbSet<CrawlResult> CrawlResults => Set<CrawlResult>();
    public DbSet<SeoAudit> SeoAudits => Set<SeoAudit>();
    public DbSet<SiteSnapshot> SiteSnapshots => Set<SiteSnapshot>();
    public DbSet<GeoAnalysis> GeoAnalyses => Set<GeoAnalysis>();
    public DbSet<AeoSchema> AeoSchemas => Set<AeoSchema>();
    public DbSet<AiContentPlan> AiContentPlans => Set<AiContentPlan>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ApiUsageLog> ApiUsageLogs => Set<ApiUsageLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Applies to DateTime and DateTime? alike; EF Core derives the nullable converter automatically.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Indexes
        modelBuilder.Entity<Website>()
            .HasIndex(w => new { w.TenantId, w.DomainUrl })
            .HasFilter("\"IsDeleted\" = false")
            .IsUnique();

        modelBuilder.Entity<TrackedKeyword>()
            .HasIndex(tk => new { tk.IsActive, tk.NextScheduledRun });

        modelBuilder.Entity<QuotaReservation>()
            .HasIndex(qr => qr.JobId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Slug)
            .IsUnique();

        // Configure Decimal Precision (PostgreSQL numeric is unbounded without an explicit precision)
        modelBuilder.Entity<Plan>()
            .Property(p => p.PriceMonthly)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ApiUsageLog>()
            .Property(a => a.EstimatedCostUsd)
            .HasPrecision(18, 6);

        // Apply Automatic Global Query Filters for IMultiTenant and ISoftDelete via generic helper
        var setQueryFilterMethod = typeof(AkironDbContext)
            .GetMethod(nameof(SetQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isMultiTenant = typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType);
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType);

            if (isMultiTenant || isSoftDelete)
            {
                var genericMethod = setQueryFilterMethod.MakeGenericMethod(entityType.ClrType);
                genericMethod.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class
    {
        var isMultiTenant = typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity));
        var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity));

        if (isMultiTenant && isSoftDelete)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => ((IMultiTenant)e).TenantId == _tenantContext.CurrentTenantId && !((ISoftDelete)e).IsDeleted);
        }
        else if (isMultiTenant)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => ((IMultiTenant)e).TenantId == _tenantContext.CurrentTenantId);
        }
        else if (isSoftDelete)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !((ISoftDelete)e).IsDeleted);
        }
    }
}
