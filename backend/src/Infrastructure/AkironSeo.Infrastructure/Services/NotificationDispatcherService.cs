using System.Net.Http.Json;
using AkironSeo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services;

public class NotificationDispatcherService : INotificationDispatcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationDispatcherService> _logger;

    public NotificationDispatcherService(HttpClient httpClient, ILogger<NotificationDispatcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task DispatchNotificationAlertAsync(
        NotificationPayloadDto payload, string? webhookUrl = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "⚡ Notification Alert [{Event}]: {Title} - {Message}",
            payload.EventType, payload.Title, payload.Message);

        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload, cts.Token);
                _logger.LogInformation("Webhook dispatched to {Url} with Status: {Status}", webhookUrl, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to dispatch Webhook to {Url}: {Error}", webhookUrl, ex.Message);
            }
        }
    }
}
