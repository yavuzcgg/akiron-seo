using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Keywords.Commands;
using AkironSeo.Application.Websites.Commands;
using AkironSeo.Application.Websites.Queries;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Services;
using MediatR;

namespace AkironSeo.API.Endpoints;

public static class WebsiteEndpoints
{
    public static void MapWebsiteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        group.MapGet("/websites", async (Guid tenantId, ITenantContext tenantContext, IMediator mediator) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await mediator.Send(new GetWebsitesQuery());
            return Results.Ok(result);
        });

        group.MapPost("/websites", async (Guid tenantId, CreateWebsiteCommand command, ITenantContext tenantContext, IMediator mediator) =>
        {
            tenantContext.SetTenantId(tenantId);
            var websiteId = await mediator.Send(command);
            return Results.Ok(new { Success = true, WebsiteId = websiteId });
        });

        group.MapPost("/websites/{id}/verify", async (Guid id, Guid tenantId, VerificationMethodEnum method, ITenantContext tenantContext, IMediator mediator) =>
        {
            tenantContext.SetTenantId(tenantId);
            var verified = await mediator.Send(new VerifyWebsiteOwnershipCommand(id, method));
            return Results.Ok(new { Success = verified, Verified = verified });
        });

        group.MapPost("/websites/{id}/crawl", async (Guid id, Guid tenantId, ITenantContext tenantContext, IWebCrawlerService crawlerService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var audit = await crawlerService.CrawlAndAuditWebsiteAsync(id, tenantId);
            return Results.Ok(new { Success = true, AuditId = audit.Id, Score = audit.OverallScore });
        });

        group.MapGet("/websites/{id}/latest-audit", async (Guid id, Guid tenantId, ITenantContext tenantContext, IMediator mediator) =>
        {
            tenantContext.SetTenantId(tenantId);
            var auditReport = await mediator.Send(new GetLatestWebsiteAuditQuery(id));
            return Results.Ok(auditReport);
        });

        group.MapPost("/keywords", async (Guid tenantId, AddTrackedKeywordCommand command, ITenantContext tenantContext, IMediator mediator) =>
        {
            tenantContext.SetTenantId(tenantId);
            var keywordId = await mediator.Send(command);
            return Results.Ok(new { Success = true, KeywordId = keywordId });
        });
    }
}
