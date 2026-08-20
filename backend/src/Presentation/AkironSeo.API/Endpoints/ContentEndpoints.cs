using AkironSeo.API.Validation;
using AkironSeo.Application.Websites.Commands;
using AkironSeo.Application.Websites.Queries;
using FluentValidation;
using MediatR;

namespace AkironSeo.API.Endpoints;

public record GenerateContentRequestDto(
    string TargetKeyword,
    string? MissingPath
);

public sealed class GenerateContentRequestValidator : AbstractValidator<GenerateContentRequestDto>
{
    public GenerateContentRequestValidator()
    {
        RuleFor(x => x.TargetKeyword).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MissingPath)
            .MaximumLength(512)
            .Must(path => string.IsNullOrWhiteSpace(path) || path.StartsWith('/'))
            .WithMessage("MissingPath must be a local path beginning with '/'.");
    }
}

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
        }).Validate<GenerateContentRequestDto>();

        group.MapGet("/{websiteId}/ai-content", async (Guid websiteId, IMediator mediator) =>
        {
            var results = await mediator.Send(new GetAiContentPlansQuery(websiteId));
            return Results.Ok(results);
        });
    }
}
