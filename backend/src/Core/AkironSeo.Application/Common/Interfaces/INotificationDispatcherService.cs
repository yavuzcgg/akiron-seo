namespace AkironSeo.Application.Common.Interfaces;

public record NotificationPayloadDto(
    string EventType, // e.g. "gold_opportunity_detected"
    Guid WebsiteId,
    string WebsiteName,
    string DomainUrl,
    string Title,
    string Message,
    DateTime Timestamp
);

public interface INotificationDispatcherService
{
    Task DispatchNotificationAlertAsync(
        NotificationPayloadDto payload,
        string? webhookUrl = null,
        CancellationToken cancellationToken = default);
}
