using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.TenantScoped;

public class Tenant : BaseEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    
    // Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties
    public ICollection<TenantUser> TenantUsers { get; set; } = new List<TenantUser>();
    public ICollection<Website> Websites { get; set; } = new List<Website>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
