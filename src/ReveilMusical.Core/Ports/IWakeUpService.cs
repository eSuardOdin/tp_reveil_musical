using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Point d'entrée du TP : l'appel déclenché à l'heure du réveil.</summary>
public interface IWakeUpService
{
    Task<WakeUpResult> SendWakeUpAsync(WakeUpRequest request, CancellationToken cancellationToken = default);
}
