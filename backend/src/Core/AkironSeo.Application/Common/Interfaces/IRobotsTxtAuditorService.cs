namespace AkironSeo.Application.Common.Interfaces;

public record AiBotStatusDto(
    string BotName,
    string UserAgent,
    string Status, // "Allowed", "Disallowed", "NotSpecified"
    string Description
);

public record RobotsTxtAuditDto(
    string DomainUrl,
    bool HasRobotsTxt,
    List<AiBotStatusDto> BotStatuses,
    string RawRobotsTxt
);

public enum RobotsTxtPresetEnum
{
    MaxAiVisibility = 1,
    SearchOnlyAi = 2,
    BlockAiTraining = 3
}

public interface IRobotsTxtAuditorService
{
    Task<RobotsTxtAuditDto> AuditRobotsTxtAsync(string domainUrl, CancellationToken cancellationToken = default);
    string GenerateOptimizedRobotsTxt(RobotsTxtPresetEnum preset, string domainUrl, string? sitemapUrl = null);
}
