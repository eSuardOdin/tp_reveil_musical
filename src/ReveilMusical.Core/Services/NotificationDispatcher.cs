using Microsoft.Extensions.Logging;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Core.Services;

/// <summary>
/// Envoie sur le canal préféré de l'utilisateur ; en cas d'échec, essaie les autres
/// canaux pour lesquels l'utilisateur a une coordonnée.
/// </summary>
public sealed class NotificationDispatcher(
    IEnumerable<INotificationChannel> channels,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task<DeliveryResult> DispatchAsync(UserProfile profile, WakeUpMessage message, CancellationToken cancellationToken = default)
    {
        var ordered = channels
            .OrderByDescending(c => string.Equals(c.Key, profile.PreferredChannel, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!ordered.Any(c => string.Equals(c.Key, profile.PreferredChannel, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning("Canal préféré '{Channel}' non disponible pour {UserId}.", profile.PreferredChannel, profile.UserId);
        }

        foreach (var channel in ordered)
        {
            var recipient = profile.ContactFor(channel.Key);
            if (string.IsNullOrWhiteSpace(recipient))
            {
                continue;
            }

            try
            {
                await channel.SendAsync(new WakeUpNotification(profile.UserId, recipient, message), cancellationToken);
                var preferred = string.Equals(channel.Key, profile.PreferredChannel, StringComparison.OrdinalIgnoreCase);
                return new DeliveryResult(channel.Key, preferred);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Échec d'envoi via {Channel} pour {UserId} ({Reason}), bascule sur le canal suivant.", channel.Key, profile.UserId, ex.Message);
                logger.LogDebug(ex, "Détail de l'échec du canal {Channel}.", channel.Key);
            }
        }

        logger.LogCritical("Aucun canal n'a pu réveiller {UserId}.", profile.UserId);
        return new DeliveryResult(null, false);
    }
}
