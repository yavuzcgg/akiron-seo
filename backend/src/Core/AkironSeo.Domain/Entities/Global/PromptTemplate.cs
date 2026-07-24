using AkironSeo.Domain.Common;
using AkironSeo.Domain.Enums;

namespace AkironSeo.Domain.Entities.Global;

public class PromptTemplate : BaseEntity
{
    public PromptTypeEnum Type { get; set; }
    public int Version { get; set; } = 1;
    public string PromptText { get; set; } = string.Empty;
    public string VariablesJson { get; set; } = "[]";
}
