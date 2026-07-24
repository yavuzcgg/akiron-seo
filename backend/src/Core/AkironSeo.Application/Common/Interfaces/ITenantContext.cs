namespace AkironSeo.Application.Common.Interfaces;

public interface ITenantContext
{
    public Guid CurrentTenantId { get; }
    public void SetTenantId(Guid tenantId);
}
