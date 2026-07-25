using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.API.Endpoints;

public record AnalyzeCompetitorRequestDto(string CompetitorDomain);

public static class CompetitorEndpoints
{
    public static void MapCompetitorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites");

        group.MapGet("/{websiteId}/competitors", async (Guid websiteId, Guid tenantId, ITenantContext tenantContext, ICompetitorService competitorService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await competitorService.GetWebsiteCompetitorsAsync(websiteId, tenantId);
            return Results.Ok(result);
        });

        group.MapPost("/{websiteId}/analyze-competitor", async (Guid websiteId, Guid tenantId, AnalyzeCompetitorRequestDto request, ITenantContext tenantContext, ICompetitorService competitorService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await competitorService.AnalyzeCompetitorGapAsync(websiteId, tenantId, request.CompetitorDomain);
            return Results.Ok(result);
        });
    }
}
