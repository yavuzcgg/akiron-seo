using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.API.Endpoints;

public static class QuotaEndpoints
{
    public static void MapQuotaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenant");

        group.MapGet("/quota", async (Guid tenantId, ITenantContext tenantContext, IQuotaLedgerService quotaLedgerService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await quotaLedgerService.GetTenantQuotaStatusAsync(tenantId);
            return Results.Ok(result);
        });
    }
}
