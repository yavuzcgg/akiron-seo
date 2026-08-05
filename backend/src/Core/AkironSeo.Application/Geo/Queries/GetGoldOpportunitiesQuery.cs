using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Geo.Queries;

public record GoldOpportunityDto(
    Guid NotificationId,
    Guid WebsiteId,
    string WebsiteName,
    string DomainUrl,
    string Title,
    string Message,
    DateTime DetectedAt,
    bool IsRead
);

public record GetGoldOpportunitiesQuery(Guid WebsiteId) : IRequest<List<GoldOpportunityDto>>;

public class GetGoldOpportunitiesQueryHandler : IRequestHandler<GetGoldOpportunitiesQuery, List<GoldOpportunityDto>>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetGoldOpportunitiesQueryHandler(IAkironDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<List<GoldOpportunityDto>> Handle(GetGoldOpportunitiesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;

        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == request.WebsiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null) return new List<GoldOpportunityDto>();

        // Scoped to the website, not just the tenant: without this every site in a
        // multi-website tenant shows the same alerts, each stamped with the wrong domain.
        var notifications = await _dbContext.Notifications
            .Where(n => n.TenantId == tenantId
                        && n.WebsiteId == request.WebsiteId
                        && n.Type == NotificationTypeEnum.GoldOpportunityAlert)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return notifications.Select(n => new GoldOpportunityDto(
            NotificationId: n.Id,
            WebsiteId: website.Id,
            WebsiteName: website.Name,
            DomainUrl: website.DomainUrl,
            Title: n.Title,
            Message: n.Message,
            DetectedAt: n.CreatedAt,
            IsRead: n.IsRead
        )).ToList();
    }
}
