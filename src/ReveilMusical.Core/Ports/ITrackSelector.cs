using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Règle métier : quel morceau rechercher pour ce profil, ce jour et cette météo.</summary>
public interface ITrackSelector
{
    string SelectQuery(UserProfile profile, WakeUpRequest request);
}
