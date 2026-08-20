using AkironSeo.API.Validation;
using AkironSeo.Application.Common.Interfaces;
using FluentValidation;

namespace AkironSeo.API.Endpoints;

public record AnalyzeCompetitorRequestDto(string CompetitorDomain);

public sealed class AnalyzeCompetitorRequestValidator : AbstractValidator<AnalyzeCompetitorRequestDto>
{
    public AnalyzeCompetitorRequestValidator()
    {
        RuleFor(x => x.CompetitorDomain)
            .NotEmpty()
            .MaximumLength(253)
            .Must(BeValidDomain).WithMessage("CompetitorDomain must contain a valid host.");
    }

    private static bool BeValidDomain(string value)
    {
        var normalized = value.Contains("://", StringComparison.Ordinal) ? value : $"https://{value}";
        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host);
    }
}

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
        }).Validate<AnalyzeCompetitorRequestDto>();
    }
}
