using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.API.Endpoints;

public static class QuotaEndpoints
{
    public static void MapQuotaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenant").RequireAuthorization();

        group.MapGet("/quota", async (ITenantContext tenantContext, IQuotaLedgerService quotaLedgerService) =>
        {
            var result = await quotaLedgerService.GetTenantQuotaStatusAsync(tenantContext.CurrentTenantId);
            return Results.Ok(result);
        });
    }
}
