using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.Global;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    // Navigation Property
    public User User { get; set; } = null!;
}
