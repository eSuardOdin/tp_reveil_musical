using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Services;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Core;

public class WeatherTrackSelectorTests
{
    private readonly WeatherTrackSelector _selector = new();

    [Fact]
    public void ReturnsTrackForWeather_WhenCovered()
    {
        var query = _selector.SelectQuery(TestData.Profile(), TestData.Request(Weather.Soleil));

        Assert.Equal("Here Comes the Sun", query);
    }

    [Fact]
    public void ReturnsFallbackTrack_WhenWeatherNotCovered()
    {
        var query = _selector.SelectQuery(TestData.Profile(), TestData.Request(Weather.Neige));

        Assert.Equal("Fallback Song", query);
    }

    [Fact]
    public void ReturnsFallbackTrack_WhenTrackIsBlank()
    {
        var profile = TestData.Profile(tracks: new Dictionary<Weather, string> { [Weather.Pluie] = "  " });

        Assert.Equal("Fallback Song", _selector.SelectQuery(profile, TestData.Request(Weather.Pluie)));
    }
}
