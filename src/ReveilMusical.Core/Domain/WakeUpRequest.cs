namespace ReveilMusical.Core.Domain;

/// <summary>Entrée de l'appel déclenché à l'heure du réveil.</summary>
public sealed record WakeUpRequest(string UserId, DayOfWeek Day, Weather Weather);
