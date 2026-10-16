using Microsoft.Extensions.Caching.Memory;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Infrastructure.Music.Decorators;

/// <summary>
/// Décorateur de cache : un même morceau demandé par de nombreux utilisateurs
/// ne coûte qu'un appel au fournisseur (indispensable avec le quota iTunes).
/// Les échecs ne sont pas mis en cache, les résultats vides le sont.
/// </summary>
public sealed class CachingMusicProvider(IMusicProvider inner, IMemoryCache cache, TimeSpan duration) : IMusicProvider
{
    public string Name => inner.Name;

    public async Task<Track?> FindTrackAsync(string query, CancellationToken cancellationToken = default)
    {
        var key = $"music:{Name}:{query.Trim().ToLowerInvariant()}";
        if (cache.TryGetValue(key, out CachedLookup? cached) && cached is not null)
        {
            return cached.Track;
        }

        var track = await inner.FindTrackAsync(query, cancellationToken);
        cache.Set(key, new CachedLookup(track), duration);
        return track;
    }

    private sealed record CachedLookup(Track? Track);
}
