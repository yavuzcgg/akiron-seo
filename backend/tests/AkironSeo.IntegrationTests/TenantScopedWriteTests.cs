using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AkironSeo.IntegrationTests;

/// <summary>
/// Covers the pipeline step that turns the access cookie's tenant_id claim into
/// ITenantContext.CurrentTenantId. Authentication and authorization are added automatically by
/// WebApplication, but TenantResolverMiddleware is custom and is not — when it was dropped from
/// Program.cs the build stayed green, the auth tests stayed green, and every tenant-scoped write
/// silently started inserting Guid.Empty and failing its Tenants foreign key with a 500.
/// These tests exercise a real write through the full HTTP pipeline so that regression cannot
/// return unnoticed.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TenantScopedWriteTests
{
    private const string ValidPassword = "StrongPassword123!";
    private const string GeminiKey = "AIzaSyIntegrationTestKeyValue";

    private readonly PostgresFixture _fixture;

    public TenantScopedWriteTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveApiKey_ShouldPersistUnderTheAuthenticatedTenant()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);
        var session = await RegisterAsync(client);

        var response = await SendWithAccessCookieAsync(
            client,
            session,
            HttpMethod.Post,
            "/api/v1/tenant/api-keys",
            new { provider = (int)AiProviderEnum.Gemini, apiKey = GeminiKey });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
        var stored = await db.EncryptedTenantApiKeys
            .IgnoreQueryFilters()
            .SingleAsync(key => key.TenantId == session.TenantId);

        Assert.NotEqual(Guid.Empty, stored.TenantId);
        Assert.Equal(AiProviderEnum.Gemini, stored.Provider);
        Assert.True(stored.IsActive);
        // The plaintext key must never reach the database.
        Assert.NotEqual(GeminiKey, stored.EncryptedKey);
    }

    [Fact]
    public async Task CreateWebsite_ShouldPersistUnderTheAuthenticatedTenant()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);
        var session = await RegisterAsync(client);
        var domain = $"tenant-scope-{Guid.NewGuid():N}.com";

        var response = await SendWithAccessCookieAsync(
            client,
            session,
            HttpMethod.Post,
            "/api/v1/websites",
            new { name = "Tenant Scope Site", domainUrl = domain });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
        var website = await db.Websites
            .IgnoreQueryFilters()
            .SingleAsync(candidate => candidate.DomainUrl == domain);

        Assert.NotEqual(Guid.Empty, website.TenantId);
        Assert.Equal(session.TenantId, website.TenantId);
    }

    [Fact]
    public async Task ListApiKeys_ShouldReturnOnlyTheCallersTenantKeys()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);

        var first = await RegisterAsync(client);
        var second = await RegisterAsync(client);

        await SendWithAccessCookieAsync(
            client,
            first,
            HttpMethod.Post,
            "/api/v1/tenant/api-keys",
            new { provider = (int)AiProviderEnum.Gemini, apiKey = GeminiKey });

        await SendWithAccessCookieAsync(
            client,
            second,
            HttpMethod.Post,
            "/api/v1/tenant/api-keys",
            new { provider = (int)AiProviderEnum.Perplexity, apiKey = "pplx-integration-test-key" });

        var firstProviders = await ReadProvidersAsync(client, first);
        var secondProviders = await ReadProvidersAsync(client, second);

        Assert.Equal([(int)AiProviderEnum.Gemini], firstProviders);
        Assert.Equal([(int)AiProviderEnum.Perplexity], secondProviders);
    }

    [Fact]
    public async Task TenantScopedWrite_ShouldBeRejectedWithoutAnAccessCookie()
    {
        await using var factory = new ApiWebApplicationFactory(_fixture.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/tenant/api-keys",
            new { provider = (int)AiProviderEnum.Gemini, apiKey = GeminiKey });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<List<int>> ReadProvidersAsync(HttpClient client, TestSession session)
    {
        var response = await SendWithAccessCookieAsync(client, session, HttpMethod.Get, "/api/v1/tenant/api-keys");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.EnumerateArray()
            .Select(entry => entry.GetProperty("provider").GetInt32())
            .OrderBy(provider => provider)
            .ToList();
    }

    private static async Task<HttpResponseMessage> SendWithAccessCookieAsync(
        HttpClient client,
        TestSession session,
        HttpMethod method,
        string path,
        object? payload = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", $"akiron_access={session.AccessToken}");
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return await client.SendAsync(request);
    }

    private static async Task<TestSession> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Tenant Scope Agency",
            fullName = "Tenant Scope Owner",
            email = $"tenant-scope-{Guid.NewGuid():N}@example.com",
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var tenantId = body.RootElement.GetProperty("tenantId").GetGuid();

        var accessCookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("akiron_access=", StringComparison.Ordinal));
        var accessToken = accessCookie.Split(';', 2)[0]["akiron_access=".Length..];

        return new TestSession(tenantId, accessToken);
    }

    private static HttpClient CreateClient(ApiWebApplicationFactory factory) => factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private sealed record TestSession(Guid TenantId, string AccessToken);
}
