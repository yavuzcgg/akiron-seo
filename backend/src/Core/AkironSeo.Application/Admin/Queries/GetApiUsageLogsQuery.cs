using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Admin.Queries;

public record ApiUsageLogDto(
    Guid LogId,
    Guid TenantId,
    string TenantName,
    string ServiceName,
    long TokensUsed,
    decimal EstimatedCostUsd,
    DateTime Timestamp
);

public record GetApiUsageLogsQuery : IRequest<List<ApiUsageLogDto>>;

public class GetApiUsageLogsQueryHandler : IRequestHandler<GetApiUsageLogsQuery, List<ApiUsageLogDto>>
{
    private readonly IAkironDbContext _dbContext;

    public GetApiUsageLogsQueryHandler(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ApiUsageLogDto>> Handle(GetApiUsageLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _dbContext.ApiUsageLogs
            .IgnoreQueryFilters()
            .OrderByDescending(l => l.Timestamp)
            .Take(50)
            .ToListAsync(cancellationToken);

        var tenantIds = logs.Select(l => l.TenantId).Distinct().ToList();

        var tenants = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        return logs.Select(l => new ApiUsageLogDto(
            LogId: l.Id,
            TenantId: l.TenantId,
            TenantName: tenants.GetValueOrDefault(l.TenantId, "Unknown Tenant"),
            ServiceName: l.ServiceName,
            TokensUsed: l.TokensUsed,
            EstimatedCostUsd: l.EstimatedCostUsd,
            Timestamp: l.Timestamp
        )).ToList();
    }
}
