using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkironSeo.Infrastructure.Persistence;

/// <summary>
/// Supplies an <see cref="AkironDbContext"/> to the EF Core design-time tools ("dotnet ef").
/// Without it the tools fall back to the API host, which would execute the startup migration and
/// seeding path just to scaffold a migration. The connection string only has to be parseable —
/// building the model never opens a connection.
/// </summary>
public class AkironDbContextFactory : IDesignTimeDbContextFactory<AkironDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=akironseo_db;Username=akiron_user;Password=akiron_password";

    public AkironDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AkironDbContext>()
            .UseNpgsql(DesignTimeConnectionString)
            .Options;

        return new AkironDbContext(options, new TenantContext());
    }
}
