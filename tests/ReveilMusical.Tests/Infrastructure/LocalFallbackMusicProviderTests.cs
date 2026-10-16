using ReveilMusical.Core.Domain;
using ReveilMusical.Infrastructure.Music.Local;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Infrastructure;

public class LocalFallbackMusicProviderTests
{
    private readonly LocalFallbackMusicProvider _provider = new();

    public static TheoryData<Weather, DayOfWeek> AllCombinations()
    {
        var data = new TheoryData<Weather, DayOfWeek>();
        foreach (var weather in Enum.GetValues<Weather>())
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            data.Add(weather, day);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public void AlwaysReturnsATrack(Weather weather, DayOfWeek day)
    {
        var track = _provider.GetTrack(TestData.Request(weather, day));

        Assert.False(string.IsNullOrWhiteSpace(track.Title));
        Assert.False(string.IsNullOrWhiteSpace(track.Artist));
    }

    [Fact]
    public void PicksTrackMatchingWeather_AndVariesWithDay()
    {
        var monday = _provider.GetTrack(TestData.Request(Weather.Pluie, DayOfWeek.Monday));
        var tuesday = _provider.GetTrack(TestData.Request(Weather.Pluie, DayOfWeek.Tuesday));

        Assert.NotEqual(monday, tuesday);
        Assert.Contains("Rain", monday.Title + tuesday.Title);
    }

    [Fact]
    public void ReturnsATrack_EvenForUnknownWeather() =>
        Assert.NotNull(_provider.GetTrack(TestData.Request((Weather)42)));

    [Fact]
    public void HasAName() => Assert.Equal("Secours local", _provider.Name);
}
