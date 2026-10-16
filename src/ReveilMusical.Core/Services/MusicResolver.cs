using Microsoft.Extensions.Logging;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Core.Services;

/// <summary>
/// Interroge les fournisseurs dans l'ordre configuré et bascule sur le suivant en cas
/// de panne ou de résultat vide. En dernier recours, la source locale garantit un morceau.
/// </summary>
public sealed class MusicResolver(
    IEnumerable<IMusicProvider> providers,
    IFallbackMusicProvider fallback,
    ILogger<MusicResolver> logger) : IMusicResolver
{
    public async Task<MusicLookupResult> ResolveAsync(string query, WakeUpRequest request, CancellationToken cancellationToken = default)
    {
        foreach (var provider in providers)
        {
            try
            {
                var track = await provider.FindTrackAsync(query, cancellationToken);
                if (track is not null)
                {
                    return new MusicLookupResult(track, provider.Name, IsFallback: false);
                }

                logger.LogInformation("Aucun résultat pour '{Query}' chez {Provider}.", query, provider.Name);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Fournisseur musical {Provider} indisponible ({Reason}), bascule sur le suivant.", provider.Name, ex.Message);
                logger.LogDebug(ex, "Détail de la panne de {Provider}.", provider.Name);
            }
        }

        logger.LogWarning("Aucun fournisseur n'a répondu pour '{Query}' : mode dégradé ({Fallback}).", query, fallback.Name);
        return new MusicLookupResult(fallback.GetTrack(request), fallback.Name, IsFallback: true);
    }
}
