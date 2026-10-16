using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Infrastructure.Profiles;

/// <summary>Mock du service interne de préférences utilisateur.</summary>
public sealed class InMemoryUserProfileProvider : IUserProfileProvider
{
    private static readonly Dictionary<string, UserProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["alice"] = new UserProfile(
            "alice",
            new Dictionary<Weather, string>
            {
                [Weather.Soleil] = "Here Comes the Sun",
                [Weather.Pluie] = "Riders on the Storm",
                [Weather.Neige] = "Let It Snow",
                [Weather.Nuageux] = "Clocks Coldplay",
            },
            FallbackTrack: "Bohemian Rhapsody",
            PreferredChannel: "email",
            Contacts: new Dictionary<string, string> { ["email"] = "alice@example.com" }),

        ["bob"] = new UserProfile(
            "bob",
            new Dictionary<Weather, string>
            {
                [Weather.Soleil] = "Walking on Sunshine",
                [Weather.Pluie] = "Purple Rain",
            },
            FallbackTrack: "Africa Toto",
            PreferredChannel: "sms",
            Contacts: new Dictionary<string, string>
            {
                ["sms"] = "+33600000001",
                ["email"] = "bob@example.com",
            }),

        ["chloe"] = new UserProfile(
            "chloe",
            new Dictionary<Weather, string>
            {
                [Weather.Neige] = "Snow Red Hot Chili Peppers",
            },
            FallbackTrack: "Get Lucky Daft Punk",
            PreferredChannel: "push",
            Contacts: new Dictionary<string, string>
            {
                ["push"] = "device-token-chloe-42",
                ["sms"] = "+33600000002",
            }),
    };

    public Task<UserProfile?> GetProfileAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Profiles.GetValueOrDefault(userId));
}
