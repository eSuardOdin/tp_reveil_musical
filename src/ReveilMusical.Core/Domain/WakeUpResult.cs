namespace ReveilMusical.Core.Domain;

/// <summary>Compte rendu d'un envoi de réveil.</summary>
/// <param name="Track">Morceau finalement envoyé.</param>
/// <param name="MusicSource">Nom du fournisseur ayant fourni le morceau.</param>
/// <param name="Channel">Canal ayant effectivement délivré le message (null si aucun).</param>
/// <param name="IsDegraded">Vrai si l'envoi a eu lieu en mode dégradé (secours musical ou autre canal que le préféré).</param>
public sealed record WakeUpResult(
    Track Track,
    string MusicSource,
    string? Channel,
    bool IsDegraded)
{
    public bool Delivered => Channel is not null;
}
