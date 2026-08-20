using AkironSeo.Application.Common.Exceptions;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Websites.Commands;

public record CreateWebsiteCommand(string Name, string DomainUrl) : IRequest<Guid>;

public sealed class CreateWebsiteCommandValidator : AbstractValidator<CreateWebsiteCommand>
{
    public CreateWebsiteCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.DomainUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeValidHttpUrl).WithMessage("DomainUrl must contain a valid HTTP or HTTPS host.");
    }

    private static bool BeValidHttpUrl(string value)
    {
        var normalized = value.Contains("://", StringComparison.Ordinal) ? value : $"https://{value}";
        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
               !string.IsNullOrWhiteSpace(uri.Host);
    }
}

public class CreateWebsiteCommandHandler : IRequestHandler<CreateWebsiteCommand, Guid>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public CreateWebsiteCommandHandler(IAkironDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> Handle(CreateWebsiteCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;

        // Clean & Normalize URL
        var normalizedUrl = request.DomainUrl.Trim().ToLowerInvariant();
        if (!normalizedUrl.StartsWith("http://") && !normalizedUrl.StartsWith("https://"))
        {
            normalizedUrl = "https://" + normalizedUrl;
        }

        var uri = new Uri(normalizedUrl);
        var cleanDomain = uri.Host;

        // Check if website domain already registered under this tenant
        var exists = await _dbContext.Websites
            .AnyAsync(w => w.TenantId == tenantId && w.DomainUrl == cleanDomain, cancellationToken);

        if (exists)
        {
            throw new ConflictException($"Website domain '{cleanDomain}' is already registered in your organization.");
        }

        var website = new Website
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            DomainUrl = cleanDomain,
            VerificationToken = $"akiron-verify-{Guid.NewGuid():N}",
            IsVerified = false
        };

        _dbContext.Websites.Add(website);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return website.Id;
    }
}
