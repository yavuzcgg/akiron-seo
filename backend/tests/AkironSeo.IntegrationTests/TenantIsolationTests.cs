using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AkironSeo.IntegrationTests;

public class TenantIsolationTests
{
    [Fact]
    public async Task GlobalQueryFilter_ShouldPreventCrossTenantDataLeakage()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AkironDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        // Seed Data for Tenant A
        var contextTenantA = new TenantContext();
        contextTenantA.SetTenantId(tenantAId);
        using (var seedContextA = new AkironDbContext(options, contextTenantA))
        {
            seedContextA.Websites.Add(new Website { TenantId = tenantAId, DomainUrl = "https://tenant-a.com", Name = "Tenant A Site" });
            await seedContextA.SaveChangesAsync();
        }

        // Seed Data for Tenant B
        var contextTenantB = new TenantContext();
        contextTenantB.SetTenantId(tenantBId);
        using (var seedContextB = new AkironDbContext(options, contextTenantB))
        {
            seedContextB.Websites.Add(new Website { TenantId = tenantBId, DomainUrl = "https://tenant-b.com", Name = "Tenant B Site" });
            await seedContextB.SaveChangesAsync();
        }

        // Act & Assert for Tenant A Query
        var queryContextA = new TenantContext();
        queryContextA.SetTenantId(tenantAId);
        using (var contextA = new AkironDbContext(options, queryContextA))
        {
            var websitesA = await contextA.Websites.ToListAsync();

            Assert.Single(websitesA);
            Assert.Equal("https://tenant-a.com", websitesA[0].DomainUrl);
        }

        // Act & Assert for Tenant B Query
        var queryContextB = new TenantContext();
        queryContextB.SetTenantId(tenantBId);
        using (var contextB = new AkironDbContext(options, queryContextB))
        {
            var websitesB = await contextB.Websites.ToListAsync();

            Assert.Single(websitesB);
            Assert.Equal("https://tenant-b.com", websitesB[0].DomainUrl);
        }
    }
}
