using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Canal de notification commun, vers lequel chaque adaptateur ramène son SDK.</summary>
public interface INotificationChannel
{
    /// <summary>Clé identifiant le canal, telle que stockée dans les préférences (ex. "email").</summary>
    string Key { get; }

    /// <summary>Envoie la notification ; lève une exception en cas d'échec.</summary>
    Task SendAsync(WakeUpNotification notification, CancellationToken cancellationToken = default);
}
