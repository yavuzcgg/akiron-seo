using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using Cronos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Keywords.Commands;

public record AddTrackedKeywordCommand(Guid WebsiteId, string Keyword, string CronExpression = "0 0 * * *") : IRequest<Guid>;

public class AddTrackedKeywordCommandHandler : IRequestHandler<AddTrackedKeywordCommand, Guid>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public AddTrackedKeywordCommandHandler(IAkironDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> Handle(AddTrackedKeywordCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == request.WebsiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new KeyNotFoundException("Website not found.");
        }

        // Validate & Parse CronExpression using Cronos library
        var cron = CronExpression.Parse(request.CronExpression);
        var nextRun = cron.GetNextOccurrence(DateTime.UtcNow) ?? DateTime.UtcNow.AddDays(1);

        var keyword = new TrackedKeyword
        {
            TenantId = tenantId,
            WebsiteId = request.WebsiteId,
            Keyword = request.Keyword.Trim(),
            CronExpression = request.CronExpression,
            IsActive = true,
            NextScheduledRun = nextRun
        };

        _dbContext.TrackedKeywords.Add(keyword);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return keyword.Id;
    }
}
