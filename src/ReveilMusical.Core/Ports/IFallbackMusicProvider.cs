using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>
/// Source de dernier recours : ne dépend d'aucun réseau et retourne toujours un morceau.
/// C'est la garantie « jamais de silence ».
/// </summary>
public interface IFallbackMusicProvider
{
    string Name { get; }

    Track GetTrack(WakeUpRequest request);
}
