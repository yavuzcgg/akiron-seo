using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace AkironSeo.IntegrationTests;

/// <summary>
/// Runs the whole suite against a throwaway PostgreSQL container so that unique indexes,
/// transactions and row-level locking behave exactly as they do in production.
/// The container is created once and the schema is applied from the real migrations.
/// Tests isolate themselves with fresh tenant ids and job ids rather than a fresh database.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    // Error detail is opt-in in Npgsql; without it constraint violations arrive without the
    // constraint name, which makes a failing test far harder to diagnose.
    public string ConnectionString => $"{_container.GetConnectionString()};Include Error Detail=true";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext(Guid.Empty);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Builds a context scoped to <paramref name="tenantId"/>, mirroring how the request
    /// pipeline resolves the tenant before any query runs.
    /// </summary>
    public AkironDbContext CreateDbContext(Guid tenantId)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenantId(tenantId);

        var options = new DbContextOptionsBuilder<AkironDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AkironDbContext(options, tenantContext);
    }

    /// <summary>
    /// Inserts the Tenant row that every tenant-scoped foreign key points at.
    /// PostgreSQL enforces these constraints for real, unlike the in-memory provider.
    /// </summary>
    public async Task SeedTenantAsync(Guid tenantId)
    {
        await using var dbContext = CreateDbContext(tenantId);

        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = $"Test Tenant {tenantId:N}",
            Slug = $"test-tenant-{tenantId:N}"
        });

        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
