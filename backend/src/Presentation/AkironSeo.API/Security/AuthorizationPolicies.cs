using AkironSeo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AkironSeo.API.Endpoints;

/// <summary>
/// Named authorization policies. The role claim is issued from <see cref="UserRoleEnum"/>
/// as its string name (see AuthEndpoints login/refresh).
/// </summary>
public static class AuthorizationPolicies
{
    public const string SuperAdminOnly = "SuperAdminOnly";

    public static void AddAkironAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(SuperAdminOnly, policy =>
                policy.RequireRole(nameof(UserRoleEnum.SuperAdmin)));
        });
    }
}
