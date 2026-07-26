using AkironSeo.Application.Common.Interfaces;
using MediatR;

namespace AkironSeo.Application.Websites.Commands;

public record GenerateAiContentCommand(
    Guid WebsiteId,
    string TargetKeyword,
    string? MissingPath = null
) : IRequest<AiContentPlanDto>;

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
