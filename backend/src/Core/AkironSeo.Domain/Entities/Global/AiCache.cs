using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.Global;

public class AiCache : BaseEntity
{
    public string HashKey { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
