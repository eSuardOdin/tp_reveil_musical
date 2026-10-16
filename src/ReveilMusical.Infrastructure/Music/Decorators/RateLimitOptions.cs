namespace ReveilMusical.Infrastructure.Music.Decorators;

/// <summary>Quota d'appels autorisés sur une fenêtre glissante.</summary>
public sealed record RateLimitOptions(int PermitLimit, TimeSpan Window);
