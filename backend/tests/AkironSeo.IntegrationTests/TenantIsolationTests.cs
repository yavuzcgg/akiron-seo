using AkironSeo.Domain.Entities.TenantScoped;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AkironSeo.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class TenantIsolationTests
{
    private readonly PostgresFixture _fixture;

    public TenantIsolationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GlobalQueryFilter_ShouldPreventCrossTenantDataLeakage()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantAId);
        await _fixture.SeedTenantAsync(tenantBId);

        await using (var seedContextA = _fixture.CreateDbContext(tenantAId))
        {
            seedContextA.Websites.Add(new Website { TenantId = tenantAId, DomainUrl = "https://tenant-a.com", Name = "Tenant A Site" });
            await seedContextA.SaveChangesAsync();
        }

        await using (var seedContextB = _fixture.CreateDbContext(tenantBId))
        {
            seedContextB.Websites.Add(new Website { TenantId = tenantBId, DomainUrl = "https://tenant-b.com", Name = "Tenant B Site" });
            await seedContextB.SaveChangesAsync();
        }

        await using (var contextA = _fixture.CreateDbContext(tenantAId))
        {
            var websitesA = await contextA.Websites.ToListAsync();

            Assert.Single(websitesA);
            Assert.Equal("https://tenant-a.com", websitesA[0].DomainUrl);
        }

        await using (var contextB = _fixture.CreateDbContext(tenantBId))
        {
            var websitesB = await contextB.Websites.ToListAsync();

            Assert.Single(websitesB);
            Assert.Equal("https://tenant-b.com", websitesB[0].DomainUrl);
        }
    }

    [Fact]
    public async Task SoftDeletedRowsShouldBeHiddenFromTheOwningTenant()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        await using (var seedContext = _fixture.CreateDbContext(tenantId))
        {
            seedContext.Websites.Add(new Website { TenantId = tenantId, DomainUrl = "https://visible.com", Name = "Visible" });
            seedContext.Websites.Add(new Website
            {
                TenantId = tenantId,
                DomainUrl = "https://deleted.com",
                Name = "Deleted",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        await using (var queryContext = _fixture.CreateDbContext(tenantId))
        {
            var websites = await queryContext.Websites.ToListAsync();

            Assert.Single(websites);
            Assert.Equal("https://visible.com", websites[0].DomainUrl);
        }
    }

    [Fact]
    public async Task DuplicateDomainForTheSameTenantShouldBeRejectedByTheDatabase()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        await using (var seedContext = _fixture.CreateDbContext(tenantId))
        {
            seedContext.Websites.Add(new Website { TenantId = tenantId, DomainUrl = "https://duplicate.com", Name = "First" });
            await seedContext.SaveChangesAsync();
        }

        // The partial unique index is what closes the check-then-insert race in CreateWebsiteCommand.
        await using (var duplicateContext = _fixture.CreateDbContext(tenantId))
        {
            duplicateContext.Websites.Add(new Website { TenantId = tenantId, DomainUrl = "https://duplicate.com", Name = "Second" });

            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
        }
    }
}
