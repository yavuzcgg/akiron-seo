namespace AkironSeo.Domain.Common;

/// <summary>
/// Defines a contract for entities supporting logical soft deletion.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been logically deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was soft deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
