namespace ReveilMusical.Core.Domain;

/// <summary>L'utilisateur demandé n'existe pas dans le service de préférences.</summary>
public sealed class UnknownUserException(string userId)
    : Exception($"Utilisateur inconnu : '{userId}'.")
{
    public string UserId { get; } = userId;
}
