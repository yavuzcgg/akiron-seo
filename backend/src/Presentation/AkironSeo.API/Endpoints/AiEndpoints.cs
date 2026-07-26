using AkironSeo.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.API.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites").RequireAuthorization();

        group.MapPost("/{id}/ai-suggestions", async (Guid id, ITenantContext tenantContext, IAiOptimizationService aiService) =>
        {
            var result = await aiService.GenerateSeoRecommendationsAsync(id, tenantContext.CurrentTenantId);
            return Results.Ok(result);
        });

        group.MapGet("/{id}/robots-txt-audit", async (Guid id, ITenantContext tenantContext, IAkironDbContext dbContext, IRobotsTxtAuditorService auditorService) =>
        {
            var website = await dbContext.Websites.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantContext.CurrentTenantId);
            if (website == null) return Results.NotFound(new { Message = "Website not found." });

            var auditResult = await auditorService.AuditRobotsTxtAsync(website.DomainUrl);
            return Results.Ok(auditResult);
        });

        group.MapGet("/{id}/aeo-schemas", async (Guid id, ITenantContext tenantContext, IAeoGeneratorService aeoService) =>
        {
            var result = await aeoService.GenerateAeoSchemasAsync(id, tenantContext.CurrentTenantId);
            return Results.Ok(result);
        });
    }
}
