using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Geo.Queries;
using MediatR;

namespace AkironSeo.API.Endpoints;

public record AnalyzePromptRequestDto(string PromptText);

public static class GeoEndpoints
{
    public static void MapGeoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites").RequireAuthorization();

        group.MapGet("/{websiteId}/geo-analysis", async (Guid websiteId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetGeoAnalysisQuery(websiteId));
            return Results.Ok(result);
        });

        group.MapPost("/{websiteId}/analyze-prompt", async (Guid websiteId, AnalyzePromptRequestDto request, ITenantContext tenantContext, IGeoEngineService geoService) =>
        {
            var result = await geoService.EvaluateCustomPromptAsync(websiteId, tenantContext.CurrentTenantId, request.PromptText);
            return Results.Ok(result);
        });
    }
}
