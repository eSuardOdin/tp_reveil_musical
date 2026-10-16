using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Services;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Core;

public class FrenchMessageComposerTests
{
    private readonly FrenchMessageComposer _composer = new();

    [Fact]
    public void Compose_UsesFrenchDayAndTrack()
    {
        var message = _composer.Compose(TestData.Request(Weather.Pluie, DayOfWeek.Wednesday), TestData.SunTrack);

        Assert.Equal("Bon mercredi !", message.Title);
        Assert.Contains("Il pleut", message.Body);
        Assert.Contains("Here Comes the Sun — The Beatles", message.Body);
        Assert.Contains("https://example.com/sun", message.Body);
        Assert.Same(TestData.SunTrack, message.Track);
    }

    [Fact]
    public void Compose_OmitsLink_WhenTrackHasNone()
    {
        var message = _composer.Compose(TestData.Request(), new Track("Titre", "Artiste"));

        Assert.DoesNotContain("Écouter", message.Body);
    }

    [Theory]
    [InlineData(Weather.Soleil, "soleil")]
    [InlineData(Weather.Pluie, "pleut")]
    [InlineData(Weather.Neige, "neige")]
    [InlineData(Weather.Nuageux, "nuageux")]
    [InlineData((Weather)99, "météo")]
    public void DescribeWeather_CoversEveryWeather(Weather weather, string expected) =>
        Assert.Contains(expected, FrenchMessageComposer.DescribeWeather(weather));
}
