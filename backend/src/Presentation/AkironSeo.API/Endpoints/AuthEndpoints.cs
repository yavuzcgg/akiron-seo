using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using AkironSeo.API.Security;
using AkironSeo.API.Validation;
using AkironSeo.Application.Auth.Dtos;
using AkironSeo.Application.Common.Exceptions;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AkironSeo.API.Endpoints;

public static partial class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-login")
            .Validate<LoginRequestDto>();

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-register")
            .Validate<RegisterRequestDto>();

        group.MapGet("/session", (ClaimsPrincipal principal) => Results.Ok(CreateSession(principal)))
            .RequireAuthorization();

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-refresh");

        group.MapPost("/logout", LogoutAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequestDto request,
        HttpContext httpContext,
        AkironDbContext db,
        IJwtTokenService jwtService,
        AuthCookieManager cookies,
        IConfiguration configuration)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.Email == email, httpContext.RequestAborted);

        if (user is null || !user.IsActive || !DbInitializer.VerifyPassword(request.Password, user.PasswordHash))
        {
            return AuthenticationFailed(httpContext);
        }

        var tenantUser = await db.TenantUsers
            .IgnoreQueryFilters()
            .Include(membership => membership.Tenant)
            .FirstOrDefaultAsync(membership => membership.UserId == user.Id, httpContext.RequestAborted);

        if (tenantUser is null || tenantUser.Tenant.IsDeleted)
        {
            return AuthenticationFailed(httpContext);
        }

        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, (DateTime?)now),
                httpContext.RequestAborted);

        var refreshToken = CreateRefreshToken(user.Id, jwtService, configuration);
        db.RefreshTokens.Add(refreshToken.Entity);
        await db.SaveChangesAsync(httpContext.RequestAborted);

        var role = tenantUser.Role.ToString();
        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, tenantUser.TenantId, role);
        cookies.WriteAccessCookie(httpContext.Response, accessToken);
        cookies.WriteRefreshCookie(httpContext.Response, refreshToken.RawToken);

        return Results.Ok(new SessionDto(user.Id, user.Email, tenantUser.TenantId, role));
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequestDto request,
        HttpContext httpContext,
        AkironDbContext db,
        IJwtTokenService jwtService,
        AuthCookieManager cookies,
        IConfiguration configuration)
    {
        var email = NormalizeEmail(request.Email);
        if (await db.Users.IgnoreQueryFilters().AnyAsync(user => user.Email == email, httpContext.RequestAborted))
        {
            throw new ConflictException("Email address is already registered.");
        }

        var defaultPlan = await db.Plans
            .IgnoreQueryFilters()
            .OrderBy(plan => plan.CreatedAt)
            .FirstOrDefaultAsync(httpContext.RequestAborted)
            ?? throw new InvalidOperationException("A default subscription plan is not configured.");

        var user = new User
        {
            Email = email,
            PasswordHash = DbInitializer.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            IsActive = true
        };

        var tenant = new Tenant { Name = request.TenantName.Trim() };
        tenant.Slug = CreateTenantSlug(tenant.Name, tenant.Id);

        var tenantUser = new TenantUser
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = UserRoleEnum.Owner
        };

        var subscription = new Subscription
        {
            TenantId = tenant.Id,
            PlanId = defaultPlan.Id,
            Status = SubscriptionStatusEnum.Active,
            MonthlyLimitTokens = 100_000,
            UsedTokens = 0
        };

        var refreshToken = CreateRefreshToken(user.Id, jwtService, configuration);

        db.Users.Add(user);
        db.Tenants.Add(tenant);
        db.TenantUsers.Add(tenantUser);
        db.Subscriptions.Add(subscription);
        db.RefreshTokens.Add(refreshToken.Entity);

        try
        {
            await db.SaveChangesAsync(httpContext.RequestAborted);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("The email address or organization identifier is already registered.");
        }

        const string role = nameof(UserRoleEnum.Owner);
        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, tenant.Id, role);
        cookies.WriteAccessCookie(httpContext.Response, accessToken);
        cookies.WriteRefreshCookie(httpContext.Response, refreshToken.RawToken);

        return Results.Created(
            "/api/v1/auth/session",
            new SessionDto(user.Id, user.Email, tenant.Id, role));
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext httpContext,
        AkironDbContext db,
        IJwtTokenService jwtService,
        AuthCookieManager cookies,
        IConfiguration configuration)
    {
        if (!httpContext.Request.Cookies.TryGetValue(AuthCookieManager.RefreshCookieName, out var rawToken) ||
            string.IsNullOrWhiteSpace(rawToken))
        {
            cookies.Clear(httpContext.Response);
            return AuthenticationFailed(httpContext);
        }

        var now = DateTime.UtcNow;
        var tokenHash = jwtService.HashRefreshToken(rawToken);
        var storedToken = await db.RefreshTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, httpContext.RequestAborted);

        if (storedToken is null)
        {
            cookies.Clear(httpContext.Response);
            return AuthenticationFailed(httpContext);
        }

        if (storedToken.RevokedAt is not null)
        {
            await RevokeFamilyAsync(db, storedToken.FamilyId, now, httpContext.RequestAborted);
            cookies.Clear(httpContext.Response);
            return AuthenticationFailed(httpContext);
        }

        if (storedToken.ExpiresAt <= now)
        {
            cookies.Clear(httpContext.Response);
            return AuthenticationFailed(httpContext);
        }

        var membership = await db.TenantUsers
            .IgnoreQueryFilters()
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Tenant)
            .FirstOrDefaultAsync(candidate => candidate.UserId == storedToken.UserId, httpContext.RequestAborted);

        if (membership is null || !membership.User.IsActive || membership.Tenant.IsDeleted)
        {
            await RevokeFamilyAsync(db, storedToken.FamilyId, now, httpContext.RequestAborted);
            cookies.Clear(httpContext.Response);
            return AuthenticationFailed(httpContext);
        }

        var replacementRawToken = jwtService.GenerateRefreshToken();
        var replacementHash = jwtService.HashRefreshToken(replacementRawToken);
        var claimedRows = await db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, (DateTime?)now)
                    .SetProperty(token => token.ReplacedByTokenHash, replacementHash),
                httpContext.RequestAborted);

        if (claimedRows == 0)
        {
            await RevokeFamilyAsync(db, storedToken.FamilyId, now, httpContext.RequestAborted);
            cookies.Clear(httpContext.Response);
            return AuthenticationFailed(httpContext);
        }

        var refreshTokenExpirationDays = configuration.GetValue("Jwt:RefreshTokenExpirationDays", 7);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = replacementHash,
            FamilyId = storedToken.FamilyId,
            ExpiresAt = now.AddDays(refreshTokenExpirationDays)
        });
        await db.SaveChangesAsync(httpContext.RequestAborted);

        var role = membership.Role.ToString();
        var accessToken = jwtService.GenerateAccessToken(
            membership.UserId,
            membership.User.Email,
            membership.TenantId,
            role);
        cookies.WriteAccessCookie(httpContext.Response, accessToken);
        cookies.WriteRefreshCookie(httpContext.Response, replacementRawToken);

        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        AkironDbContext db,
        IJwtTokenService jwtService,
        AuthCookieManager cookies)
    {
        if (httpContext.Request.Cookies.TryGetValue(AuthCookieManager.RefreshCookieName, out var rawToken) &&
            !string.IsNullOrWhiteSpace(rawToken))
        {
            var tokenHash = jwtService.HashRefreshToken(rawToken);
            var familyId = await db.RefreshTokens
                .IgnoreQueryFilters()
                .Where(token => token.TokenHash == tokenHash)
                .Select(token => (Guid?)token.FamilyId)
                .FirstOrDefaultAsync(httpContext.RequestAborted);

            if (familyId.HasValue)
            {
                await RevokeFamilyAsync(db, familyId.Value, DateTime.UtcNow, httpContext.RequestAborted);
            }
        }

        cookies.Clear(httpContext.Response);
        return Results.NoContent();
    }

    private static (RefreshToken Entity, string RawToken) CreateRefreshToken(
        Guid userId,
        IJwtTokenService jwtService,
        IConfiguration configuration)
    {
        var rawToken = jwtService.GenerateRefreshToken();
        var expirationDays = configuration.GetValue("Jwt:RefreshTokenExpirationDays", 7);

        return (
            new RefreshToken
            {
                UserId = userId,
                TokenHash = jwtService.HashRefreshToken(rawToken),
                FamilyId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
            },
            rawToken);
    }

    private static Task<int> RevokeFamilyAsync(
        AkironDbContext db,
        Guid familyId,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        return db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, (DateTime?)revokedAt),
                cancellationToken);
    }

    private static SessionDto CreateSession(ClaimsPrincipal principal)
    {
        var userId = ParseGuidClaim(principal, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);
        var tenantId = ParseGuidClaim(principal, "tenant_id");
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                    ?? principal.FindFirstValue(ClaimTypes.Email)
                    ?? throw new UnauthorizedAccessException("The session email claim is missing.");
        var role = principal.FindFirstValue(ClaimTypes.Role)
                   ?? throw new UnauthorizedAccessException("The session role claim is missing.");

        return new SessionDto(userId, email, tenantId, role);
    }

    private static Guid ParseGuidClaim(ClaimsPrincipal principal, params string[] claimTypes)
    {
        var value = claimTypes
            .Select(principal.FindFirstValue)
            .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("A required session claim is invalid.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string CreateTenantSlug(string tenantName, Guid tenantId)
    {
        var withoutDiacritics = string.Concat(
            tenantName.Normalize(NormalizationForm.FormD)
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
        var baseSlug = Regex.Replace(withoutDiacritics.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "tenant";
        }

        return $"{baseSlug}-{tenantId.ToString("N")[..8]}";
    }

    private static IResult AuthenticationFailed(HttpContext context) => Results.Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Authentication failed",
        detail: "Invalid credentials or inactive session.",
        extensions: new Dictionary<string, object?>
        {
            ["correlationId"] = context.Response.Headers["X-Correlation-ID"].ToString()
        });
}
