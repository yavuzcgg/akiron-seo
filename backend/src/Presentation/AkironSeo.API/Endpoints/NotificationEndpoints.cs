using AkironSeo.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.API.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").RequireAuthorization();

        group.MapGet("/", async (ITenantContext tenantContext, IAkironDbContext dbContext) =>
        {
            var tenantId = tenantContext.CurrentTenantId;
            var notifications = await dbContext.Notifications
                .Where(n => n.TenantId == tenantId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(notifications);
        });

        group.MapPost("/{id}/read", async (Guid id, ITenantContext tenantContext, IAkironDbContext dbContext) =>
        {
            var tenantId = tenantContext.CurrentTenantId;
            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId);

            if (notification == null) return Results.NotFound();

            notification.IsRead = true;
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { Success = true });
        });
    }
}
