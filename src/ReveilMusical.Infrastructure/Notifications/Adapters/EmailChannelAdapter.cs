using System.Net;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Infrastructure.Notifications.Vendors;

namespace ReveilMusical.Infrastructure.Notifications.Adapters;

/// <summary>Ramène le SDK email (synchrone, HTML) à l'interface commune.</summary>
public sealed class EmailChannelAdapter(IMailSender mailSender) : INotificationChannel
{
    public const string ChannelKey = "email";

    public string Key => ChannelKey;

    public Task SendAsync(WakeUpNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var message = notification.Message;
        var html = $"<p>{WebUtility.HtmlEncode(message.Body)}</p>";
        mailSender.SendMail(notification.Recipient, message.Title, html);
        return Task.CompletedTask;
    }
}
