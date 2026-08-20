using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AkironSeo.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ApiAuthenticationTests
{
    private const string ValidPassword = "StrongPassword123!";
    private readonly PostgresFixture _fixture;

    public ApiAuthenticationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_ShouldSetHttpOnlyCookiesWithoutReturningTokens()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);

        var response = await RegisterAsync(client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);

        var cookieHeaders = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookieHeaders, value => HasCookieFlags(value, "akiron_access", "/api"));
        Assert.Contains(cookieHeaders, value => HasCookieFlags(value, "akiron_refresh", "/api/v1/auth"));

        var sessionRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/session");
        sessionRequest.Headers.Add("Cookie", CreateCookieHeader(cookieHeaders));
        var sessionResponse = await client.SendAsync(sessionRequest);
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldStoreOnlyTheRefreshTokenHash()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);
        var email = NewEmail();

        var response = await RegisterAsync(client, email);
        var rawToken = ExtractCookie(response, "akiron_refresh");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Email == email);
        var storedToken = await db.RefreshTokens.IgnoreQueryFilters().SingleAsync(token => token.UserId == user.Id);
        var jwtService = scope.ServiceProvider.GetRequiredService<AkironSeo.Application.Common.Interfaces.IJwtTokenService>();

        Assert.NotEqual(rawToken, storedToken.TokenHash);
        Assert.Equal(jwtService.HashRefreshToken(rawToken), storedToken.TokenHash);
        Assert.Equal(64, storedToken.TokenHash.Length);
    }

    [Fact]
    public async Task Refresh_ShouldRotateTokenAndRejectReuseOfTheOldTokenFamily()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);
        var registerResponse = await RegisterAsync(client);
        var originalRefreshToken = ExtractCookie(registerResponse, "akiron_refresh");

        var refreshRequest = CreateCookieRequest(HttpMethod.Post, "/api/v1/auth/refresh", "akiron_refresh", originalRefreshToken);
        var refreshResponse = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.NoContent, refreshResponse.StatusCode);
        var replacementRefreshToken = ExtractCookie(refreshResponse, "akiron_refresh");
        Assert.NotEqual(originalRefreshToken, replacementRefreshToken);

        var reuseRequest = CreateCookieRequest(HttpMethod.Post, "/api/v1/auth/refresh", "akiron_refresh", originalRefreshToken);
        var reuseResponse = await client.SendAsync(reuseRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        var replacementRequest = CreateCookieRequest(HttpMethod.Post, "/api/v1/auth/refresh", "akiron_refresh", replacementRefreshToken);
        var replacementResponse = await client.SendAsync(replacementRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeTheRefreshFamilyAndClearCookies()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);
        var registerResponse = await RegisterAsync(client);
        var refreshToken = ExtractCookie(registerResponse, "akiron_refresh");

        var logoutRequest = CreateCookieRequest(HttpMethod.Post, "/api/v1/auth/logout", "akiron_refresh", refreshToken);
        var logoutResponse = await client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        var clearHeaders = logoutResponse.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(clearHeaders, value => value.StartsWith("akiron_access=", StringComparison.Ordinal));
        Assert.Contains(clearHeaders, value => value.StartsWith("akiron_refresh=", StringComparison.Ordinal));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
        Assert.All(
            await db.RefreshTokens.IgnoreQueryFilters().Where(token => token.RevokedAt != null).ToListAsync(),
            token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task ExistingAccessCookie_ShouldFailImmediatelyAfterTenantIsDisabled()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);
        var email = NewEmail();
        var registerResponse = await RegisterAsync(client, email);
        var accessToken = ExtractCookie(registerResponse, "akiron_access");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
            var user = await db.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Email == email);
            var tenantId = await db.TenantUsers.IgnoreQueryFilters()
                .Where(membership => membership.UserId == user.Id)
                .Select(membership => membership.TenantId)
                .SingleAsync();
            var tenant = await db.Tenants.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == tenantId);
            tenant.IsDeleted = true;
            tenant.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var websitesRequest = CreateCookieRequest(HttpMethod.Get, "/api/v1/websites", "akiron_access", accessToken);
        var websitesResponse = await client.SendAsync(websitesRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, websitesResponse.StatusCode);
    }

    [Fact]
    public async Task ExistingAccessCookie_ShouldFailImmediatelyAfterRoleChanges()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);
        var email = NewEmail();
        var registerResponse = await RegisterAsync(client, email);
        var accessToken = ExtractCookie(registerResponse, "akiron_access");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
            var user = await db.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Email == email);
            var membership = await db.TenantUsers.IgnoreQueryFilters().SingleAsync(candidate => candidate.UserId == user.Id);
            membership.Role = UserRoleEnum.Member;
            await db.SaveChangesAsync();
        }

        var websitesRequest = CreateCookieRequest(HttpMethod.Get, "/api/v1/websites", "akiron_access", accessToken);
        var websitesResponse = await client.SendAsync(websitesRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, websitesResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnValidationProblemForWeakPassword()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Test Agency",
            fullName = "Test Owner",
            email = NewEmail(),
            password = "weak"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("password", out _));
        Assert.True(body.RootElement.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task Login_ShouldReturnTooManyRequestsAfterConfiguredLimit()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "2",
            ["RateLimiting:Login:WindowMinutes"] = "60"
        };
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString, overrides);
        using var client = CreateClient(factory);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = $"missing-{attempt}@example.com",
                password = ValidPassword
            });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var limitedResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "missing-final@example.com",
            password = ValidPassword
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
    }

    private static HttpClient CreateClient(ApiWebApplicationFactory factory) => factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string? email = null) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Integration Test Agency",
            fullName = "Integration Test Owner",
            email = email ?? NewEmail(),
            password = ValidPassword
        });

    private static HttpRequestMessage CreateCookieRequest(HttpMethod method, string path, string name, string value)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", $"{name}={value}");
        return request;
    }

    private static string ExtractCookie(HttpResponseMessage response, string cookieName)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{cookieName}=", StringComparison.Ordinal));
        return header.Split(';', 2)[0][(cookieName.Length + 1)..];
    }

    private static string CreateCookieHeader(IEnumerable<string> setCookieHeaders) => string.Join(
        "; ",
        setCookieHeaders.Select(header => header.Split(';', 2)[0]));

    private static bool HasCookieFlags(string value, string cookieName, string path) =>
        value.StartsWith($"{cookieName}=", StringComparison.Ordinal) &&
        value.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
        value.Contains("secure", StringComparison.OrdinalIgnoreCase) &&
        value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase) &&
        value.Contains($"path={path}", StringComparison.OrdinalIgnoreCase);

    private static string NewEmail() => $"integration-{Guid.NewGuid():N}@example.com";
}
