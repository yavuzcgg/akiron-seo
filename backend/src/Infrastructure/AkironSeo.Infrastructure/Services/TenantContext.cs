using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    private Guid _currentTenantId;

    public Guid CurrentTenantId => _currentTenantId;

    public void SetTenantId(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }
}
