namespace ReveilMusical.Core.Domain;

/// <summary>Résultat de la distribution d'une notification sur les canaux disponibles.</summary>
public sealed record DeliveryResult(string? Channel, bool UsedPreferredChannel)
{
    public bool Delivered => Channel is not null;
}
