using System.Text;

namespace AkironSeo.API.Security;

/// <summary>
/// Fails startup when required secrets are absent, too weak, or still set to the
/// values shipped in appsettings.Development.json. Booting a non-development host
/// with development keys would let anyone holding the repository forge tokens and
/// decrypt every tenant's stored provider API key.
/// </summary>
public static class SecretsValidator
{
    /// <summary>HS256 needs at least 256 bits of key material.</summary>
    private const int MinimumJwtKeyBytes = 32;

    private static readonly string[] DevelopmentPlaceholders =
    [
        "AkironSeo-Dev-Only-Jwt-Secret-Key-Must-Be-At-Least-256-Bits!",
        "AkironSeo-Dev-Only-Master-Encryption-Key-2026!"
    ];

    public static void ValidateSecrets(this IConfiguration configuration, IHostEnvironment environment)
    {
        var failures = new List<string>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            failures.Add("ConnectionStrings__DefaultConnection is not set.");
        }

        var jwtSecret = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            failures.Add("Jwt__SecretKey is not set.");
        }
        else if (Encoding.UTF8.GetByteCount(jwtSecret) < MinimumJwtKeyBytes)
        {
            failures.Add($"Jwt__SecretKey must be at least {MinimumJwtKeyBytes} bytes for HMAC-SHA256.");
        }

        var masterKey = configuration["Security:MasterEncryptionKey"];
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            failures.Add("Security__MasterEncryptionKey is not set.");
        }

        if (!environment.IsDevelopment())
        {
            foreach (var value in new[] { jwtSecret, masterKey, connectionString })
            {
                if (value is not null && DevelopmentPlaceholders.Any(p => value.Contains(p, StringComparison.Ordinal)))
                {
                    failures.Add("A development placeholder secret is configured. Supply real secrets via environment variables.");
                    break;
                }
            }
        }

        var cookieSecure = configuration.GetValue<bool?>("Auth:CookieSecure") ?? !environment.IsDevelopment();
        if (!cookieSecure)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            var onlyLoopbackOrigins = allowedOrigins.Length > 0 && allowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback);

            if (!onlyLoopbackOrigins)
            {
                failures.Add("Auth__CookieSecure may be false only when every allowed origin is loopback.");
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"Startup aborted — invalid secret configuration in the '{environment.EnvironmentName}' environment:{Environment.NewLine}" +
                string.Join(Environment.NewLine, failures.Select(f => $"  - {f}")));
        }
    }
}
