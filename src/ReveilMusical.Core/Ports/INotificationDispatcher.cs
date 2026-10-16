using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Distribue un message sur le canal préféré, puis sur les autres en cas d'échec.</summary>
public interface INotificationDispatcher
{
    Task<DeliveryResult> DispatchAsync(UserProfile profile, WakeUpMessage message, CancellationToken cancellationToken = default);
}
