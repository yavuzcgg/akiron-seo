using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Keywords.Commands;
using AkironSeo.Application.Keywords.Queries;
using MediatR;

namespace AkironSeo.API.Endpoints;

public static class KeywordEndpoints
{
    public static void MapKeywordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        group.MapGet("/websites/{websiteId}/keywords", async (Guid websiteId, Guid tenantId, ITenantContext tenantContext, IMediator mediator) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await mediator.Send(new GetTrackedKeywordsQuery(websiteId));
            return Results.Ok(result);
        });

        group.MapPost("/keywords/{id}/check-rank", async (Guid id, Guid tenantId, ITenantContext tenantContext, IKeywordRankTrackerService trackerService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await trackerService.CheckKeywordRankAsync(id, tenantId);
            return Results.Ok(result);
        });
    }
}
