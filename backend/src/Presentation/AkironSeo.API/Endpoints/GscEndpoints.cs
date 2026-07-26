using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.API.Endpoints;

public static class GscEndpoints
{
    public static void MapGscEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites").RequireAuthorization();

        group.MapGet("/{websiteId}/gsc-analytics", async (Guid websiteId, ITenantContext tenantContext, ISearchConsoleService gscService) =>
        {
            var result = await gscService.GetSearchConsoleAnalyticsAsync(websiteId, tenantContext.CurrentTenantId);
            return Results.Ok(result);
        });
    }
}
