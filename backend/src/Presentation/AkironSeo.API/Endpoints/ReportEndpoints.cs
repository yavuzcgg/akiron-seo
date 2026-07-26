using AkironSeo.Application.Websites.Queries;
using MediatR;

namespace AkironSeo.API.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites").RequireAuthorization();

        group.MapGet("/{websiteId}/export-report", async (Guid websiteId, IMediator mediator) =>
        {
            var report = await mediator.Send(new GetExecutiveReportQuery(websiteId));
            return Results.Content(report.HtmlReportDocument, "text/html; charset=utf-8");
        });
    }
}
