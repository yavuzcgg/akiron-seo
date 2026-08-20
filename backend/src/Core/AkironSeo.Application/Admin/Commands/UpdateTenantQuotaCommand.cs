using AkironSeo.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Admin.Commands;

public record UpdateTenantQuotaCommand(
    Guid TenantId,
    long NewMonthlyLimitTokens,
    bool ResetUsedTokens = false
) : IRequest<bool>;

public sealed class UpdateTenantQuotaCommandValidator : AbstractValidator<UpdateTenantQuotaCommand>
{
    public UpdateTenantQuotaCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.NewMonthlyLimitTokens).InclusiveBetween(1, 10_000_000_000);
    }
}

public class UpdateTenantQuotaCommandHandler : IRequestHandler<UpdateTenantQuotaCommand, bool>
{
    private readonly IAkironDbContext _dbContext;

    public UpdateTenantQuotaCommandHandler(IAkironDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateTenantQuotaCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId, cancellationToken);

        if (subscription == null)
        {
            // If subscription doesn't exist yet, fetch plan or create default
            var defaultPlan = await _dbContext.Plans
                .FirstOrDefaultAsync(cancellationToken);

            subscription = new Domain.Entities.TenantScoped.Subscription
            {
                TenantId = request.TenantId,
                PlanId = defaultPlan?.Id ?? Guid.NewGuid(),
                MonthlyLimitTokens = request.NewMonthlyLimitTokens,
                UsedTokens = 0,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            };

            _dbContext.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.MonthlyLimitTokens = request.NewMonthlyLimitTokens;
            if (request.ResetUsedTokens)
            {
                subscription.UsedTokens = 0;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
