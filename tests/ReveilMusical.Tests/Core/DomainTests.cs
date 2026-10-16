using ReveilMusical.Core.Domain;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Core;

public class DomainTests
{
    [Fact]
    public void Track_ToString_ShowsTitleAndArtist() =>
        Assert.Equal("Here Comes the Sun — The Beatles", TestData.SunTrack.ToString());

    [Fact]
    public void UserProfile_ContactFor_ReturnsContactOrNull()
    {
        var profile = TestData.Profile();

        Assert.Equal("u1@example.com", profile.ContactFor("email"));
        Assert.Null(profile.ContactFor("push"));
    }

    [Theory]
    [InlineData("email", true)]
    [InlineData(null, false)]
    public void Results_DeliveredDependsOnChannel(string? channel, bool expected)
    {
        Assert.Equal(expected, new WakeUpResult(TestData.SunTrack, "src", channel, false).Delivered);
        Assert.Equal(expected, new DeliveryResult(channel, false).Delivered);
    }

    [Fact]
    public void UnknownUserException_KeepsUserId()
    {
        var ex = new UnknownUserException("zoe");

        Assert.Equal("zoe", ex.UserId);
        Assert.Contains("zoe", ex.Message);
    }
}
