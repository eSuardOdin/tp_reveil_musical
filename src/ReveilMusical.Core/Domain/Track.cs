namespace ReveilMusical.Core.Domain;

/// <summary>
/// Morceau tel que le métier le connaît : aucune donnée propre à un fournisseur
/// (trackViewUrl, artist-credit…) n'apparaît ici.
/// </summary>
/// <param name="Title">Titre du morceau.</param>
/// <param name="Artist">Artiste principal.</param>
/// <param name="ListenUrl">Lien d'écoute éventuel, quel que soit le fournisseur.</param>
public sealed record Track(string Title, string Artist, Uri? ListenUrl = null)
{
    public override string ToString() => $"{Title} — {Artist}";
}
