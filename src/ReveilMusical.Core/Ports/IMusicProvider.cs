using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Source de morceaux (iTunes, MusicBrainz, …). Peut échouer : c'est attendu.</summary>
public interface IMusicProvider
{
    /// <summary>Nom lisible du fournisseur (journalisation, compte rendu).</summary>
    string Name { get; }

    /// <summary>Recherche un morceau. Retourne null si rien ne correspond ; lève une exception en cas de panne.</summary>
    Task<Track?> FindTrackAsync(string query, CancellationToken cancellationToken = default);
}
