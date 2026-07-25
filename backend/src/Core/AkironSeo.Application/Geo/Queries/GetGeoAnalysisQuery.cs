using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Geo.Queries;

public record GetGeoAnalysisQuery(Guid WebsiteId) : IRequest<GeoAnalysisResultDto?>;

public class GetGeoAnalysisQueryHandler : IRequestHandler<GetGeoAnalysisQuery, GeoAnalysisResultDto?>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IGeoEngineService _geoEngineService;

    public GetGeoAnalysisQueryHandler(
        IAkironDbContext dbContext,
        ITenantContext tenantContext,
        IGeoEngineService geoEngineService)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _geoEngineService = geoEngineService;
    }

    public async Task<GeoAnalysisResultDto?> Handle(GetGeoAnalysisQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;

        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == request.WebsiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null) return null;

        return await _geoEngineService.AnalyzeBrandGeoVisibilityAsync(request.WebsiteId, tenantId, cancellationToken);
    }
}
