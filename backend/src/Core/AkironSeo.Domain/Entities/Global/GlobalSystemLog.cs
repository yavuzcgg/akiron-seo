using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.Global;

public class GlobalSystemLog : BaseEntity
{
    public string LogLevel { get; set; } = "Information";
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? CorrelationId { get; set; }
}
