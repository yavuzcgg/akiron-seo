using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Keywords.Queries;

public record GetTrackedKeywordsQuery(Guid WebsiteId) : IRequest<List<TrackedKeywordDto>>;

public class GetTrackedKeywordsQueryHandler : IRequestHandler<GetTrackedKeywordsQuery, List<TrackedKeywordDto>>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetTrackedKeywordsQueryHandler(IAkironDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<List<TrackedKeywordDto>> Handle(GetTrackedKeywordsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;

        var keywords = await _dbContext.TrackedKeywords
            .Where(k => k.WebsiteId == request.WebsiteId && k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

        return keywords.Select(k =>
        {
            var prevPos = k.PreviousPosition ?? k.CurrentPosition ?? 0;
            var currPos = k.CurrentPosition ?? 0;
            var change = (prevPos > 0 && currPos > 0) ? (prevPos - currPos) : 0; // Positive means rank improved (e.g. 5 -> 3 = +2)

            return new TrackedKeywordDto(
                Id: k.Id,
                WebsiteId: k.WebsiteId,
                KeywordText: k.Keyword,
                TargetCountry: k.TargetCountry,
                TargetLanguage: k.Language,
                CurrentPosition: k.CurrentPosition,
                PreviousPosition: k.PreviousPosition,
                PositionChange: change,
                TargetUrl: k.TargetUrl,
                IsActive: k.IsActive,
                LastCheckedAt: k.LastCheckedAt,
                NextScheduledRun: k.NextScheduledRun
            );
        }).ToList();
    }
}
