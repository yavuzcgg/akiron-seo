using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Admin.Commands;

public record PruneSystemLogsCommand(int OlderThanDays = 30) : IRequest<int>;

public class PruneSystemLogsCommandHandler : IRequestHandler<PruneSystemLogsCommand, int>
{
    private readonly IAkironDbContext _dbContext;

    public PruneSystemLogsCommandHandler(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> Handle(PruneSystemLogsCommand request, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-request.OlderThanDays);

        // Delete GlobalSystemLogs older than cutoff
        var oldSystemLogs = await _dbContext.GlobalSystemLogs
            .Where(l => l.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        _dbContext.GlobalSystemLogs.RemoveRange(oldSystemLogs);

        // Delete old ApiUsageLogs older than cutoff
        var oldUsageLogs = await _dbContext.ApiUsageLogs
            .IgnoreQueryFilters()
            .Where(l => l.Timestamp < cutoff)
            .ToListAsync(cancellationToken);

        _dbContext.ApiUsageLogs.RemoveRange(oldUsageLogs);

        int totalPruned = oldSystemLogs.Count + oldUsageLogs.Count;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return totalPruned;
    }
}
