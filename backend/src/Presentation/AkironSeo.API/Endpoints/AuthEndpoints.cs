using AkironSeo.Application.Auth.Dtos;
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
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (LoginRequestDto request, AkironDbContext db) =>
        {
            var user = await db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !DbInitializer.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.BadRequest(new AuthResponseDto(false, "Invalid email or password.", null, null, null, null));
            }

            var tenantUser = await db.TenantUsers
                .IgnoreQueryFilters()
                .Include(tu => tu.Tenant)
                .FirstOrDefaultAsync(tu => tu.UserId == user.Id);

            var role = tenantUser?.Role.ToString() ?? "Member";
            var tenantId = tenantUser?.TenantId ?? Guid.Empty;

            return Results.Ok(new AuthResponseDto(
                Success: true,
                Message: "Login successful.",
                AccessToken: $"mock_jwt_token_{user.Id}",
                TenantId: tenantId,
                UserEmail: user.Email,
                Role: role
            ));
        });

        group.MapPost("/register", async (RegisterRequestDto request, AkironDbContext db) =>
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

            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponseDto(
                Success: true,
                Message: "Account and Organization created successfully.",
                AccessToken: $"mock_jwt_token_{user.Id}",
                TenantId: tenant.Id,
                UserEmail: user.Email,
                Role: "Owner"
            ));
        });
    }
}
