using AkironSeo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Admin.Commands;

public record ToggleTenantStatusCommand(Guid TenantId) : IRequest<bool>;

public class ToggleTenantStatusCommandHandler : IRequestHandler<ToggleTenantStatusCommand, bool>
{
    private readonly IAkironDbContext _dbContext;

    public ToggleTenantStatusCommandHandler(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ToggleTenantStatusCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null) return false;

        // Toggle active status (soft delete / restore)
        tenant.IsDeleted = !tenant.IsDeleted;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return !tenant.IsDeleted;
    }
}
