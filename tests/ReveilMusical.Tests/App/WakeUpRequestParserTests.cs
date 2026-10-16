using ReveilMusical.App;
using ReveilMusical.Core.Domain;

namespace ReveilMusical.Tests.App;

public class WakeUpRequestParserTests
{
    [Theory]
    [InlineData("SOLEIL", Weather.Soleil)]
    [InlineData("pluie", Weather.Pluie)]
    [InlineData(" Neige ", Weather.Neige)]
    [InlineData("NUAGEUX", Weather.Nuageux)]
    public void ParsesWeather(string input, Weather expected) =>
        Assert.Equal(expected, WakeUpRequestParser.ParseWeather(input));

    [Theory]
    [InlineData("ORAGE")]
    [InlineData("7")]
    public void RejectsUnknownWeather(string input) =>
        Assert.Throws<ArgumentException>(() => WakeUpRequestParser.ParseWeather(input));

    [Theory]
    [InlineData("lundi", DayOfWeek.Monday)]
    [InlineData("DIMANCHE", DayOfWeek.Sunday)]
    [InlineData("Saturday", DayOfWeek.Saturday)]
    [InlineData("3", DayOfWeek.Wednesday)]
    public void ParsesDay(string input, DayOfWeek expected) =>
        Assert.Equal(expected, WakeUpRequestParser.ParseDay(input));

    [Theory]
    [InlineData("funday")]
    [InlineData("12")]
    public void RejectsUnknownDay(string input) =>
        Assert.Throws<ArgumentException>(() => WakeUpRequestParser.ParseDay(input));

    [Fact]
    public void ParsesFullRequest() =>
        Assert.Equal(new WakeUpRequest("alice", DayOfWeek.Friday, Weather.Pluie), WakeUpRequestParser.Parse(" alice ", "vendredi", "PLUIE"));

    [Fact]
    public void RejectsBlankUserId() =>
        Assert.Throws<ArgumentException>(() => WakeUpRequestParser.Parse("", "lundi", "SOLEIL"));
}
