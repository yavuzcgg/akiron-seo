using AkironSeo.Application.Admin.Commands;
using AkironSeo.Application.Admin.Queries;
using MediatR;

namespace AkironSeo.API.Endpoints;

public record UpdateQuotaRequestDto(long NewMonthlyLimitTokens, bool ResetUsedTokens = false);
public record PruneLogsRequestDto(int OlderThanDays = 30);

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Every handler in this group calls IgnoreQueryFilters(), so the tenant isolation
        // filter offers no protection here. The SuperAdmin policy is the only boundary.
        var group = app.MapGroup("/api/v1/admin").RequireAuthorization(AuthorizationPolicies.SuperAdminOnly);

        group.MapGet("/tenants", async (IMediator mediator) =>
        {
            var tenants = await mediator.Send(new GetAdminTenantsQuery());
            return Results.Ok(tenants);
        });

        group.MapPost("/tenants/{tenantId}/quota", async (Guid tenantId, UpdateQuotaRequestDto request, IMediator mediator) =>
        {
            var command = new UpdateTenantQuotaCommand(tenantId, request.NewMonthlyLimitTokens, request.ResetUsedTokens);
            var success = await mediator.Send(command);
            return Results.Ok(new { Success = success });
        });

        group.MapPost("/tenants/{tenantId}/toggle-status", async (Guid tenantId, IMediator mediator) =>
        {
            var activeStatus = await mediator.Send(new ToggleTenantStatusCommand(tenantId));
            return Results.Ok(new { Success = true, IsActive = activeStatus });
        });

        group.MapGet("/usage-logs", async (IMediator mediator) =>
        {
            var logs = await mediator.Send(new GetApiUsageLogsQuery());
            return Results.Ok(logs);
        });

        group.MapPost("/prune-logs", async (PruneLogsRequestDto request, IMediator mediator) =>
        {
            var prunedCount = await mediator.Send(new PruneSystemLogsCommand(request.OlderThanDays));
            return Results.Ok(new { Success = true, PrunedRecordsCount = prunedCount });
        });
    }
}
