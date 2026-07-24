using System.Security.Cryptography;
using System.Text;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AkironDbContext context)
    {
        // Apply migrations automatically if any
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        // Seed Plans if empty
        if (!await context.Plans.IgnoreQueryFilters().AnyAsync())
        {
            var plan = new Plan
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Agency Unlimited Plan",
                PriceMonthly = 149.00m,
                LimitsJson = "{\"max_websites\": 50, \"max_keywords\": 500, \"monthly_ai_tokens\": 1000000}"
            };
            context.Plans.Add(plan);
            await context.SaveChangesAsync();
        }

        // Seed SuperAdmin User & HQ Tenant if empty
        if (!await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "admin@akironseo.com"))
        {
            var superAdminUser = new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "admin@akironseo.com",
                PasswordHash = HashPassword("Admin123!"),
                FullName = "Akiron SuperAdmin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(superAdminUser);

            var hqTenant = new Tenant
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Akiron HQ",
                Slug = "akiron-hq",
                CreatedAt = DateTime.UtcNow
            };
            context.Tenants.Add(hqTenant);

            var tenantUser = new TenantUser
            {
                TenantId = hqTenant.Id,
                UserId = superAdminUser.Id,
                Role = UserRoleEnum.SuperAdmin,
                JoinedAt = DateTime.UtcNow
            };
            context.TenantUsers.Add(tenantUser);

            var plan = await context.Plans.IgnoreQueryFilters().FirstAsync();
            var subscription = new Subscription
            {
                TenantId = hqTenant.Id,
                PlanId = plan.Id,
                Status = SubscriptionStatusEnum.Active,
                MonthlyLimitTokens = 1000000,
                UsedTokens = 0,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddYears(1)
            };
            context.Subscriptions.Add(subscription);

            await context.SaveChangesAsync();
        }
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
