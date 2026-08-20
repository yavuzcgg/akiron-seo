using AkironSeo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AkironSeo.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly IReadOnlyDictionary<string, string?> _overrides;

    public ApiWebApplicationFactory(
        string connectionString,
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        _connectionString = connectionString;
        _overrides = overrides ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _connectionString,
            ["Jwt:SecretKey"] = "Integration-Test-Jwt-Key-With-More-Than-Thirty-Two-Bytes!",
            ["Security:MasterEncryptionKey"] = "Integration-Test-Master-Key-With-More-Than-Thirty-Two-Bytes!",
            ["Auth:CookieSecure"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000",
            ["RateLimiting:Login:PermitLimit"] = "1000",
            ["RateLimiting:Register:PermitLimit"] = "1000",
            ["RateLimiting:Refresh:PermitLimit"] = "1000"
        };

        foreach (var entry in _overrides)
        {
            settings[entry.Key] = entry.Value;
        }

        foreach (var entry in settings)
        {
            builder.UseSetting(entry.Key, entry.Value);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AkironDbContext>>();
            services.RemoveAll<AkironDbContext>();
            services.AddDbContext<AkironDbContext>(options => options.UseNpgsql(_connectionString));
        });
    }
}
