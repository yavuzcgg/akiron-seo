using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Keywords.Commands;
using AkironSeo.Application.Websites.Commands;
using AkironSeo.Application.Websites.Queries;
using AkironSeo.Domain.Enums;
using MediatR;

namespace AkironSeo.API.Endpoints;

public static class WebsiteEndpoints
{
    public static void MapWebsiteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/websites", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWebsitesQuery());
            return Results.Ok(result);
        });

        group.MapPost("/websites", async (CreateWebsiteCommand command, IMediator mediator) =>
        {
            var websiteId = await mediator.Send(command);
            return Results.Ok(new { Success = true, WebsiteId = websiteId });
        });

        group.MapPost("/websites/{id}/verify", async (Guid id, VerificationMethodEnum method, IMediator mediator) =>
        {
            var verified = await mediator.Send(new VerifyWebsiteOwnershipCommand(id, method));
            return Results.Ok(new { Success = verified, Verified = verified });
        });

        group.MapPost("/websites/{id}/crawl", async (Guid id, ITenantContext tenantContext, IWebCrawlerService crawlerService) =>
        {
            var audit = await crawlerService.CrawlAndAuditWebsiteAsync(id, tenantContext.CurrentTenantId);
            return Results.Ok(new { Success = true, AuditId = audit.Id, Score = audit.OverallScore });
        });

        group.MapGet("/websites/{id}/latest-audit", async (Guid id, IMediator mediator) =>
        {
            var auditReport = await mediator.Send(new GetLatestWebsiteAuditQuery(id));
            return Results.Ok(auditReport);
        });

        group.MapPost("/keywords", async (AddTrackedKeywordCommand command, IMediator mediator) =>
        {
            var keywordId = await mediator.Send(command);
            return Results.Ok(new { Success = true, KeywordId = keywordId });
        });
    }
}
