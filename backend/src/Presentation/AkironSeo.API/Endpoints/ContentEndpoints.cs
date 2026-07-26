using AkironSeo.Application.Websites.Commands;
using AkironSeo.Application.Websites.Queries;
using MediatR;

namespace AkironSeo.API.Endpoints;

public record GenerateContentRequestDto(
    string TargetKeyword,
    string? MissingPath
);

public static class ContentEndpoints
{
    public static void MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites").RequireAuthorization();

        group.MapPost("/{websiteId}/ai-content/generate", async (Guid websiteId, GenerateContentRequestDto request, IMediator mediator) =>
        {
            var command = new GenerateAiContentCommand(websiteId, request.TargetKeyword, request.MissingPath);
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("/{websiteId}/ai-content", async (Guid websiteId, IMediator mediator) =>
        {
            var results = await mediator.Send(new GetAiContentPlansQuery(websiteId));
            return Results.Ok(results);
        });
    }
}
