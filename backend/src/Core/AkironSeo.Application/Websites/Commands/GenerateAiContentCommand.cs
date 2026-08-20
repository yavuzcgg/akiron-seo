using AkironSeo.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace AkironSeo.Application.Websites.Commands;

public record GenerateAiContentCommand(
    Guid WebsiteId,
    string TargetKeyword,
    string? MissingPath = null
) : IRequest<AiContentPlanDto>;

public sealed class GenerateAiContentCommandValidator : AbstractValidator<GenerateAiContentCommand>
{
    public GenerateAiContentCommandValidator()
    {
        RuleFor(x => x.WebsiteId).NotEmpty();
        RuleFor(x => x.TargetKeyword).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MissingPath)
            .MaximumLength(512)
            .Must(path => string.IsNullOrWhiteSpace(path) || path.StartsWith('/'))
            .WithMessage("MissingPath must be a local path beginning with '/'.");
    }
}

public class GenerateAiContentCommandHandler : IRequestHandler<GenerateAiContentCommand, AiContentPlanDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly IAiContentWriterService _contentWriterService;

    public GenerateAiContentCommandHandler(
        ITenantContext tenantContext,
        IAiContentWriterService contentWriterService)
    {
        _tenantContext = tenantContext;
        _contentWriterService = contentWriterService;
    }

    public async Task<AiContentPlanDto> Handle(GenerateAiContentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        return await _contentWriterService.GenerateGeoContentAsync(
            request.WebsiteId, tenantId, request.TargetKeyword, request.MissingPath, cancellationToken);
    }
}
