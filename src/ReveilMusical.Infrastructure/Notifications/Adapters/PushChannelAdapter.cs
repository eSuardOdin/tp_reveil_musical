using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Infrastructure.Notifications.Vendors;

namespace ReveilMusical.Infrastructure.Notifications.Adapters;

/// <summary>Ramène le SDK push (payload + accusé) à l'interface commune.</summary>
public sealed class PushChannelAdapter(IPushService pushService) : INotificationChannel
{
    public const string ChannelKey = "push";

    public string Key => ChannelKey;

    public Task SendAsync(WakeUpNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var message = notification.Message;
        var data = new Dictionary<string, string>
        {
            ["title"] = message.Track.Title,
            ["artist"] = message.Track.Artist,
        };
        if (message.Track.ListenUrl is not null)
        {
            data["url"] = message.Track.ListenUrl.ToString();
        }

        var receipt = pushService.Notify(notification.Recipient, new PushPayload(message.Title, message.Body, data));
        if (!receipt.Accepted)
        {
            throw new NotificationFailedException(Key, receipt.Error ?? "refusé");
        }

        return Task.CompletedTask;
    }
}
