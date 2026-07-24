using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Websites.Queries;

public record WebsiteDto(Guid Id, string Name, string DomainUrl, bool IsVerified, string VerificationToken, DateTime CreatedAt);

public record GetWebsitesQuery() : IRequest<List<WebsiteDto>>;

public class GetWebsitesQueryHandler : IRequestHandler<GetWebsitesQuery, List<WebsiteDto>>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetWebsitesQueryHandler(IAkironDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<List<WebsiteDto>> Handle(GetWebsitesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;

        return await _dbContext.Websites
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WebsiteDto(w.Id, w.Name, w.DomainUrl, w.IsVerified, w.VerificationToken, w.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
