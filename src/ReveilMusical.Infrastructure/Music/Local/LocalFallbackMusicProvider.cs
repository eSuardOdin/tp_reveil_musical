using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Infrastructure.Music.Local;

/// <summary>
/// Liste de morceaux codée en dur, sans réseau : utilisée quand aucun fournisseur
/// n'est disponible. Le choix dépend de la météo puis du jour de la semaine.
/// </summary>
public sealed class LocalFallbackMusicProvider : IFallbackMusicProvider
{
    private static readonly IReadOnlyList<(Weather Weather, Track Track)> Catalog =
    [
        (Weather.Soleil, new Track("Here Comes the Sun", "The Beatles")),
        (Weather.Soleil, new Track("Walking on Sunshine", "Katrina and the Waves")),
        (Weather.Pluie, new Track("Singin' in the Rain", "Gene Kelly")),
        (Weather.Pluie, new Track("Have You Ever Seen the Rain", "Creedence Clearwater Revival")),
        (Weather.Neige, new Track("Let It Snow!", "Dean Martin")),
        (Weather.Neige, new Track("Snow (Hey Oh)", "Red Hot Chili Peppers")),
        (Weather.Nuageux, new Track("Both Sides Now", "Joni Mitchell")),
        (Weather.Nuageux, new Track("Mr. Blue Sky", "Electric Light Orchestra")),
    ];

    public string Name => "Secours local";

    public Track GetTrack(WakeUpRequest request)
    {
        var candidates = Catalog.Where(e => e.Weather == request.Weather).Select(e => e.Track).ToList();
        if (candidates.Count == 0)
        {
            candidates = Catalog.Select(e => e.Track).ToList();
        }

        return candidates[(int)request.Day % candidates.Count];
    }
}
