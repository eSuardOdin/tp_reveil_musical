namespace ReveilMusical.Core.Domain;

/// <summary>
/// Préférences d'un utilisateur, telles que retournées par le service interne.
/// </summary>
/// <param name="UserId">Identifiant de l'utilisateur.</param>
/// <param name="TracksByWeather">Morceau choisi pour chaque type de météo (peut être incomplet).</param>
/// <param name="FallbackTrack">Morceau de secours pour les météos non couvertes.</param>
/// <param name="PreferredChannel">Clé du canal préféré (ex. "email") : une simple chaîne, le métier ne connaît pas la liste des canaux.</param>
/// <param name="Contacts">Coordonnées par clé de canal (ex. "sms" → numéro).</param>
public sealed record UserProfile(
    string UserId,
    IReadOnlyDictionary<Weather, string> TracksByWeather,
    string FallbackTrack,
    string PreferredChannel,
    IReadOnlyDictionary<string, string> Contacts)
{
    public string? ContactFor(string channelKey) =>
        Contacts.TryGetValue(channelKey, out var contact) ? contact : null;
}
