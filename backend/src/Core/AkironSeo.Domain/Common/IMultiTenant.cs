namespace AkironSeo.Domain.Common;

/// <summary>
/// Defines a contract for entities scoped to a specific tenant in a multi-tenant system.
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant owning this entity.
    /// </summary>
    public Guid TenantId { get; set; }
}
