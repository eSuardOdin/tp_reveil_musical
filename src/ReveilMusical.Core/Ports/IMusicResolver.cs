using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Trouve toujours un morceau, en passant d'un fournisseur à l'autre si besoin.</summary>
public interface IMusicResolver
{
    Task<MusicLookupResult> ResolveAsync(string query, WakeUpRequest request, CancellationToken cancellationToken = default);
}
