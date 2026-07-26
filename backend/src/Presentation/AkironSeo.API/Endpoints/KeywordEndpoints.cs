using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Keywords.Commands;
using AkironSeo.Application.Keywords.Queries;
using MediatR;

namespace AkironSeo.API.Endpoints;

public static class KeywordEndpoints
{
    public static void MapKeywordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/websites/{websiteId}/keywords", async (Guid websiteId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetTrackedKeywordsQuery(websiteId));
            return Results.Ok(result);
        });

        group.MapPost("/keywords/{id}/check-rank", async (Guid id, ITenantContext tenantContext, IKeywordRankTrackerService trackerService) =>
        {
            var result = await trackerService.CheckKeywordRankAsync(id, tenantContext.CurrentTenantId);
            return Results.Ok(result);
        });
    }
}
