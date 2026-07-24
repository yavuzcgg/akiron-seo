using AkironSeo.Domain.Common;

namespace AkironSeo.Domain.Entities.Global;

public class Plan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public string LimitsJson { get; set; } = "{}";
}
