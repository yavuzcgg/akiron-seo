using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.Global;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    // Navigation Property
    public User User { get; set; } = null!;
}
