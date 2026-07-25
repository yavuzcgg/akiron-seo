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

public interface IRobotsTxtAuditorService
{
    Task<RobotsTxtAuditDto> AuditRobotsTxtAsync(string domainUrl, CancellationToken cancellationToken = default);
}
