using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.API.Endpoints;

public record AnalyzeCompetitorRequestDto(string CompetitorDomain);

public static class CompetitorEndpoints
{
    public static void MapCompetitorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites").RequireAuthorization();

        group.MapGet("/{websiteId}/competitors", async (Guid websiteId, ITenantContext tenantContext, ICompetitorService competitorService) =>
        {
            var result = await competitorService.GetWebsiteCompetitorsAsync(websiteId, tenantContext.CurrentTenantId);
            return Results.Ok(result);
        });

        group.MapPost("/{websiteId}/analyze-competitor", async (Guid websiteId, AnalyzeCompetitorRequestDto request, ITenantContext tenantContext, ICompetitorService competitorService) =>
        {
            var result = await competitorService.AnalyzeCompetitorGapAsync(websiteId, tenantContext.CurrentTenantId, request.CompetitorDomain);
            return Results.Ok(result);
        });
    }
}
