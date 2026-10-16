namespace ReveilMusical.Core.Domain;

/// <summary>Morceau trouvé et nom du fournisseur qui l'a fourni.</summary>
public sealed record MusicLookupResult(Track Track, string Source, bool IsFallback);
