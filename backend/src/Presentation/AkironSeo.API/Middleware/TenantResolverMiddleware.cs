using System.Security.Claims;
using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.API.Middleware;

/// <summary>
/// Extracts the tenant_id claim from the authenticated user's JWT and sets it on ITenantContext.
/// Anonymous requests (auth endpoints) leave the tenant context at Guid.Empty,
/// which is safe: the global query filter on Guid.Empty returns no tenant rows.
/// </summary>
public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id");
            if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantId))
            {
                tenantContext.SetTenantId(tenantId);
            }
        }

        await _next(context);
    }
}
