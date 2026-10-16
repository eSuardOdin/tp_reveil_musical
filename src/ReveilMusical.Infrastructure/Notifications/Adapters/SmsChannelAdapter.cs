using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Infrastructure.Notifications.Vendors;

namespace ReveilMusical.Infrastructure.Notifications.Adapters;

/// <summary>
/// Ramène le SDK SMS (codes de statut, 160 caractères max) à l'interface commune :
/// le texte est raccourci et un code d'erreur devient une exception.
/// </summary>
public sealed class SmsChannelAdapter(ISmsGateway gateway) : INotificationChannel
{
    public const string ChannelKey = "sms";

    public string Key => ChannelKey;

    public async Task SendAsync(WakeUpNotification notification, CancellationToken cancellationToken = default)
    {
        var text = BuildText(notification.Message);
        var status = await gateway.SubmitAsync(new SmsPayload(notification.Recipient, text), cancellationToken);
        if (status is < 200 or >= 300)
        {
            throw new NotificationFailedException(Key, $"code {status}");
        }
    }

    internal static string BuildText(WakeUpMessage message)
    {
        var text = $"{message.Title} {message.Track}";
        return text.Length <= ISmsGateway.MaxLength ? text : string.Concat(text.AsSpan(0, ISmsGateway.MaxLength - 1), "…");
    }
}
