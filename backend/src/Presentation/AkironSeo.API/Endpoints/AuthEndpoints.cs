using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkironSeo.Application.Auth.Dtos;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").AllowAnonymous();

        group.MapPost("/login", async (LoginRequestDto request, AkironDbContext db, IJwtTokenService jwtService, IConfiguration config) =>
        {
            var user = await db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !DbInitializer.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.BadRequest(new AuthResponseDto(false, "Invalid email or password.", null, null, null, null));
            }

            // Deactivated accounts get the same message as bad credentials so the response
            // does not reveal which addresses are registered.
            if (!user.IsActive)
            {
                return Results.BadRequest(new AuthResponseDto(false, "Invalid email or password.", null, null, null, null));
            }

            var tenantUser = await db.TenantUsers
                .IgnoreQueryFilters()
                .Include(tu => tu.Tenant)
                .FirstOrDefaultAsync(tu => tu.UserId == user.Id);

            var role = tenantUser?.Role.ToString() ?? "Member";
            var tenantId = tenantUser?.TenantId ?? Guid.Empty;

            var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, tenantId, role);
            var refreshTokenValue = jwtService.GenerateRefreshToken();

            // Revoke any existing refresh tokens for this user
            await db.RefreshTokens
                .IgnoreQueryFilters()
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true));

            var refreshTokenExpirationDays = config.GetValue("Jwt:RefreshTokenExpirationDays", 7);

            // Save new refresh token
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                IsRevoked = false
            });
            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponseDto(
                Success: true,
                Message: "Login successful.",
                AccessToken: accessToken,
                TenantId: tenantId,
                UserEmail: user.Email,
                Role: role
            ));
        });

        group.MapPost("/register", async (RegisterRequestDto request, AkironDbContext db, IJwtTokenService jwtService, IConfiguration config) =>
        {
            var existingUser = await db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == request.Email);

            if (existingUser)
            {
                return Results.BadRequest(new AuthResponseDto(false, "Email address already registered.", null, null, null, null));
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = DbInitializer.HashPassword(request.Password),
                FullName = request.FullName,
                IsActive = true
            };
            db.Users.Add(user);

            var tenant = new Tenant
            {
                Name = request.TenantName,
                Slug = request.TenantName.ToLowerInvariant().Replace(" ", "-")
            };
            db.Tenants.Add(tenant);

            var tenantUser = new TenantUser
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = UserRoleEnum.Owner
            };
            db.TenantUsers.Add(tenantUser);

            var defaultPlan = await db.Plans.IgnoreQueryFilters().FirstOrDefaultAsync();
            if (defaultPlan != null)
            {
                var subscription = new Subscription
                {
                    TenantId = tenant.Id,
                    PlanId = defaultPlan.Id,
                    Status = SubscriptionStatusEnum.Active,
                    MonthlyLimitTokens = 100000,
                    UsedTokens = 0
                };
                db.Subscriptions.Add(subscription);
            }

            var refreshTokenExpirationDays = config.GetValue("Jwt:RefreshTokenExpirationDays", 7);
            var refreshTokenValue = jwtService.GenerateRefreshToken();

            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                IsRevoked = false
            });

            await db.SaveChangesAsync();

            var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, tenant.Id, "Owner");

            return Results.Ok(new AuthResponseDto(
                Success: true,
                Message: "Account and Organization created successfully.",
                AccessToken: accessToken,
                TenantId: tenant.Id,
                UserEmail: user.Email,
                Role: "Owner"
            ));
        });

        group.MapPost("/refresh", async (RefreshTokenRequestDto request, AkironDbContext db, IJwtTokenService jwtService, IConfiguration config) =>
        {
            var principal = jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal is null)
            {
                return Results.Unauthorized();
            }

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                              ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Results.Unauthorized();
            }

            var storedToken = await db.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(rt =>
                    rt.UserId == userId &&
                    rt.Token == request.RefreshToken &&
                    !rt.IsRevoked &&
                    rt.ExpiresAt > DateTime.UtcNow);

            if (storedToken is null)
            {
                return Results.Unauthorized();
            }

            // Revoke used token (rotation)
            storedToken.IsRevoked = true;

            // Look up tenant and role for new token
            var tenantUser = await db.TenantUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(tu => tu.UserId == userId);

            var user = await db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null || !user.IsActive)
            {
                return Results.Unauthorized();
            }

            var role = tenantUser?.Role.ToString() ?? "Member";
            var tenantId = tenantUser?.TenantId ?? Guid.Empty;

            var newAccessToken = jwtService.GenerateAccessToken(userId, user.Email, tenantId, role);
            var newRefreshToken = jwtService.GenerateRefreshToken();

            var refreshTokenExpirationDays = config.GetValue("Jwt:RefreshTokenExpirationDays", 7);

            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                IsRevoked = false
            });

            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponseDto(
                Success: true,
                Message: "Token refreshed successfully.",
                AccessToken: newAccessToken,
                TenantId: tenantId,
                UserEmail: user.Email,
                Role: role
            ));
        });
    }
}
