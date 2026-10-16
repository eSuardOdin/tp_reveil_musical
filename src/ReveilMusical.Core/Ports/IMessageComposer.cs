using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Rédige le message de réveil.</summary>
public interface IMessageComposer
{
    WakeUpMessage Compose(WakeUpRequest request, Track track);
}
