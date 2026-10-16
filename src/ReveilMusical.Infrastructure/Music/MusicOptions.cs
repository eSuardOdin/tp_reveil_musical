namespace ReveilMusical.Infrastructure.Music;

public sealed class MusicOptions
{
    public const string SectionName = "Music";

    /// <summary>
    /// Fournisseurs à interroger, dans l'ordre. Changer de fournisseur = changer cette liste
    /// dans la configuration, sans toucher au code. Liste vide = secours local uniquement.
    /// </summary>
    public List<string> Providers { get; set; } = [];

    /// <summary>Durée de cache d'une recherche (préserve les quotas des API).</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(12);
}
