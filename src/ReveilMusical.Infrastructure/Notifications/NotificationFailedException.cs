namespace ReveilMusical.Infrastructure.Notifications;

/// <summary>Le SDK d'un canal a refusé l'envoi.</summary>
public sealed class NotificationFailedException(string channel, string reason)
    : Exception($"Échec d'envoi via '{channel}' : {reason}")
{
    public string Channel { get; } = channel;
}
