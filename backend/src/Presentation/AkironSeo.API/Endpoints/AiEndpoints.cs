using AkironSeo.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.API.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/websites");

        group.MapPost("/{id}/ai-suggestions", async (Guid id, Guid tenantId, ITenantContext tenantContext, IAiOptimizationService aiService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await aiService.GenerateSeoRecommendationsAsync(id, tenantId);
            return Results.Ok(result);
        });

        group.MapGet("/{id}/robots-txt-audit", async (Guid id, Guid tenantId, ITenantContext tenantContext, IAkironDbContext dbContext, IRobotsTxtAuditorService auditorService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var website = await dbContext.Websites.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);
            if (website == null) return Results.NotFound(new { Message = "Website not found." });

            var auditResult = await auditorService.AuditRobotsTxtAsync(website.DomainUrl);
            return Results.Ok(auditResult);
        });

        group.MapGet("/{id}/aeo-schemas", async (Guid id, Guid tenantId, ITenantContext tenantContext, IAeoGeneratorService aeoService) =>
        {
            tenantContext.SetTenantId(tenantId);
            var result = await aeoService.GenerateAeoSchemasAsync(id, tenantId);
            return Results.Ok(result);
        });
    }
}
