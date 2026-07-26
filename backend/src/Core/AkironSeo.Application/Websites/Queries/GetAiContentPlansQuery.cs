using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Websites.Queries;

public record GetAiContentPlansQuery(Guid WebsiteId) : IRequest<List<AiContentPlanDto>>;

public class GetAiContentPlansQueryHandler : IRequestHandler<GetAiContentPlansQuery, List<AiContentPlanDto>>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetAiContentPlansQueryHandler(IAkironDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<List<AiContentPlanDto>> Handle(GetAiContentPlansQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;

        var plans = await _dbContext.AiContentPlans
            .Where(c => c.WebsiteId == request.WebsiteId && c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return plans.Select(p => new AiContentPlanDto(
            Id: p.Id,
            WebsiteId: p.WebsiteId,
            TargetKeyword: p.TargetKeyword,
            MissingPath: null,
            GeneratedMarkdownContent: p.GeneratedMarkdownContent,
            Status: p.Status,
            TokensSpent: p.TokensSpent,
            CreatedAt: p.CreatedAt
        )).ToList();
    }
}
