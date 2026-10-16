namespace ReveilMusical.Core.Domain;

/// <summary>Contenu du message de réveil, indépendant du canal.</summary>
public sealed record WakeUpMessage(string Title, string Body, Track Track);

/// <summary>Message de réveil adressé à un destinataire sur un canal donné.</summary>
/// <param name="UserId">Utilisateur à réveiller.</param>
/// <param name="Recipient">Coordonnée propre au canal (adresse, numéro, jeton…), opaque pour le métier.</param>
/// <param name="Message">Contenu du message.</param>
public sealed record WakeUpNotification(string UserId, string Recipient, WakeUpMessage Message);
