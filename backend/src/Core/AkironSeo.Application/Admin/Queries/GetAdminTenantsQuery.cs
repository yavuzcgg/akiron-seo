using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Admin.Queries;

public record AdminTenantDto(
    Guid TenantId,
    string TenantName,
    string Slug,
    string PlanName,
    long MonthlyLimitTokens,
    long UsedTokens,
    int RegisteredWebsitesCount,
    bool IsActive,
    DateTime CreatedAt
);

public record GetAdminTenantsQuery : IRequest<List<AdminTenantDto>>;

public class GetAdminTenantsQueryHandler : IRequestHandler<GetAdminTenantsQuery, List<AdminTenantDto>>
{
    private readonly IAkironDbContext _dbContext;

    public GetAdminTenantsQueryHandler(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AdminTenantDto>> Handle(GetAdminTenantsQuery request, CancellationToken cancellationToken)
    {
        // Bypass global query filters to retrieve system-wide tenant information
        var tenants = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var tenantIds = tenants.Select(t => t.Id).ToList();

        var subscriptions = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Where(s => tenantIds.Contains(s.TenantId))
            .ToListAsync(cancellationToken);

        var websiteCounts = await _dbContext.Websites
            .IgnoreQueryFilters()
            .Where(w => tenantIds.Contains(w.TenantId) && !w.IsDeleted)
            .GroupBy(w => w.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var dtos = new List<AdminTenantDto>();

        foreach (var tenant in tenants)
        {
            var sub = subscriptions.FirstOrDefault(s => s.TenantId == tenant.Id);
            var siteCount = websiteCounts.GetValueOrDefault(tenant.Id, 0);

            dtos.Add(new AdminTenantDto(
                TenantId: tenant.Id,
                TenantName: tenant.Name,
                Slug: tenant.Slug,
                PlanName: sub?.Plan?.Name ?? "Standard B2B Plan",
                MonthlyLimitTokens: sub?.MonthlyLimitTokens ?? 500000,
                UsedTokens: sub?.UsedTokens ?? 0,
                RegisteredWebsitesCount: siteCount,
                IsActive: !tenant.IsDeleted,
                CreatedAt: tenant.CreatedAt
            ));
        }

        return dtos;
    }
}
