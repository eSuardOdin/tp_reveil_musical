namespace ReveilMusical.Infrastructure.Notifications.Vendors;

/// <summary>Permet de simuler la panne d'un canal mocké (démonstration du mode dégradé).</summary>
public sealed class MockOutageOptions
{
    public const string SectionName = "Notifications:Mocks";

    /// <summary>Clés des canaux simulés en panne (ex. "sms").</summary>
    public List<string> Outages { get; set; } = [];

    public bool IsDown(string channel) => Outages.Contains(channel, StringComparer.OrdinalIgnoreCase);
}
