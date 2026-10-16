using ReveilMusical.Core.Domain;

namespace ReveilMusical.Tests.Helpers;

internal static class TestData
{
    public static readonly Track SunTrack = new("Here Comes the Sun", "The Beatles", new Uri("https://example.com/sun"));

    public static WakeUpRequest Request(Weather weather = Weather.Soleil, DayOfWeek day = DayOfWeek.Monday, string userId = "u1") =>
        new(userId, day, weather);

    public static UserProfile Profile(
        string preferredChannel = "email",
        IReadOnlyDictionary<Weather, string>? tracks = null,
        IReadOnlyDictionary<string, string>? contacts = null,
        string fallbackTrack = "Fallback Song") =>
        new(
            "u1",
            tracks ?? new Dictionary<Weather, string> { [Weather.Soleil] = "Here Comes the Sun" },
            fallbackTrack,
            preferredChannel,
            contacts ?? new Dictionary<string, string> { ["email"] = "u1@example.com", ["sms"] = "+33600000000" });

    public static WakeUpMessage Message(Track? track = null) => new("Bon lundi !", "Il fait beau.", track ?? SunTrack);
}
