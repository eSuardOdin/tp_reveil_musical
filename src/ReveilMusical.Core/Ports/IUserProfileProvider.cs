using ReveilMusical.Core.Domain;

namespace ReveilMusical.Core.Ports;

/// <summary>Service interne retournant les préférences d'un utilisateur.</summary>
public interface IUserProfileProvider
{
    /// <summary>Retourne le profil, ou null si l'utilisateur est inconnu.</summary>
    Task<UserProfile?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
}
