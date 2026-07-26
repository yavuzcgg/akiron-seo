using AkironSeo.Application.Common.Interfaces;
using MediatR;

namespace AkironSeo.Application.Websites.Queries;

public record GetExecutiveReportQuery(Guid WebsiteId) : IRequest<ExecutiveReportDto>;

public class GetExecutiveReportQueryHandler : IRequestHandler<GetExecutiveReportQuery, ExecutiveReportDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly IReportExportService _reportExportService;

    public GetExecutiveReportQueryHandler(
        ITenantContext tenantContext,
        IReportExportService reportExportService)
    {
        _tenantContext = tenantContext;
        _reportExportService = reportExportService;
    }

    public async Task<ExecutiveReportDto> Handle(GetExecutiveReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        return await _reportExportService.GenerateExecutiveReportAsync(request.WebsiteId, tenantId, cancellationToken);
    }
}
