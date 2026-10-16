using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Infrastructure.Music.Decorators;

/// <summary>
/// Décorateur qui fait respecter le quota d'un fournisseur (fenêtre glissante).
/// Quota atteint : on n'attend pas (le réveil est à heure fixe), on lève une exception
/// pour que le résolveur passe immédiatement au fournisseur suivant.
/// </summary>
public sealed class RateLimitedMusicProvider(IMusicProvider inner, RateLimitOptions limits, TimeProvider timeProvider) : IMusicProvider
{
    private readonly Queue<DateTimeOffset> _calls = new();
    private readonly Lock _lock = new();

    public string Name => inner.Name;

    public Task<Track?> FindTrackAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!TryAcquire())
        {
            throw new ProviderUnavailableException(Name, $"quota de {limits.PermitLimit} appels / {limits.Window} atteint");
        }

        return inner.FindTrackAsync(query, cancellationToken);
    }

    private bool TryAcquire()
    {
        var now = timeProvider.GetUtcNow();
        lock (_lock)
        {
            while (_calls.Count > 0 && now - _calls.Peek() >= limits.Window)
            {
                _calls.Dequeue();
            }

            if (_calls.Count >= limits.PermitLimit)
            {
                return false;
            }

            _calls.Enqueue(now);
            return true;
        }
    }
}
