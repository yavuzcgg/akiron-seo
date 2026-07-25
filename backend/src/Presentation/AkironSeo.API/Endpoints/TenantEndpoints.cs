using AkironSeo.Application.Auth.Dtos;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.API.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenant");

        group.MapPost("/api-keys", async (SaveApiKeyDto request, ITenantContext tenantContext, AkironDbContext db, IApiKeyEncryptionService encryptionService) =>
        {
            tenantContext.SetTenantId(request.TenantId);

            var encryptedKey = encryptionService.Encrypt(request.ApiKey);
            var existing = await db.EncryptedTenantApiKeys
                .FirstOrDefaultAsync(k => k.TenantId == request.TenantId && k.Provider == request.Provider);

            if (existing != null)
            {
                existing.EncryptedKey = encryptedKey;
                existing.IsActive = true;
            }
            else
            {
                db.EncryptedTenantApiKeys.Add(new EncryptedTenantApiKey
                {
                    TenantId = request.TenantId,
                    Provider = request.Provider,
                    EncryptedKey = encryptedKey,
                    IsActive = true
                });
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Success = true, Message = $"BYOK Encrypted API key for {request.Provider} saved successfully." });
        });
    }
}
