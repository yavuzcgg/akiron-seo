using AkironSeo.API.Validation;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Geo.Queries;
using FluentValidation;
using MediatR;

namespace AkironSeo.API.Endpoints;

public record AnalyzePromptRequestDto(string PromptText);

public sealed class AnalyzePromptRequestValidator : AbstractValidator<AnalyzePromptRequestDto>
{
    public AnalyzePromptRequestValidator()
    {
        RuleFor(x => x.PromptText).NotEmpty().MaximumLength(2000);
    }
}

public static class GeoEndpoints
{
    public static void MapGeoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites").RequireAuthorization();

        group.MapGet("/{websiteId}/geo-analysis", async (Guid websiteId, bool? forceRefresh, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetGeoAnalysisQuery(websiteId, forceRefresh ?? false));
            return Results.Ok(result);
        });

        group.MapPost("/{websiteId}/analyze-prompt", async (Guid websiteId, AnalyzePromptRequestDto request, ITenantContext tenantContext, IGeoEngineService geoService) =>
        {
            var result = await geoService.EvaluateCustomPromptAsync(websiteId, tenantContext.CurrentTenantId, request.PromptText, forceRefresh: true);
            return Results.Ok(result);
        }).Validate<AnalyzePromptRequestDto>();

        group.MapGet("/{websiteId}/gold-opportunities", async (Guid websiteId, IMediator mediator) =>
        {
            var opportunities = await mediator.Send(new GetGoldOpportunitiesQuery(websiteId));
            return Results.Ok(opportunities);
        });
    }
}
