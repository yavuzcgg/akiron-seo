using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using Cronos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Keywords.Commands;

public record AddTrackedKeywordCommand(
    Guid WebsiteId,
    string Keyword,
    string Language = "en",
    string TargetCountry = "US",
    string CronExpression = "0 0 * * *") : IRequest<Guid>;

public sealed class AddTrackedKeywordCommandValidator : AbstractValidator<AddTrackedKeywordCommand>
{
    public AddTrackedKeywordCommandValidator()
    {
        RuleFor(x => x.WebsiteId).NotEmpty();
        RuleFor(x => x.Keyword).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Language).Matches("^[A-Za-z]{2}$");
        RuleFor(x => x.TargetCountry).Matches("^[A-Za-z]{2}$");
        RuleFor(x => x.CronExpression)
            .NotEmpty()
            .MaximumLength(100)
            .Must(BeValidCron).WithMessage("CronExpression must be a valid five-part cron expression.");
    }

    private static bool BeValidCron(string expression)
    {
        try
        {
            CronExpression.Parse(expression);
            return true;
        }
        catch (CronFormatException)
        {
            return false;
        }
    }
}

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
            Language = request.Language.ToLowerInvariant(),
            TargetCountry = request.TargetCountry.ToUpperInvariant(),
            CronExpression = request.CronExpression,
            IsActive = true,
            NextScheduledRun = nextRun
        };

        _dbContext.TrackedKeywords.Add(keyword);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return keyword.Id;
    }
}
