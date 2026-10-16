using ReveilMusical.Core.Domain;
using ReveilMusical.Infrastructure.Profiles;

namespace ReveilMusical.Tests.Infrastructure;

public class InMemoryUserProfileProviderTests
{
    private readonly InMemoryUserProfileProvider _provider = new();

    [Theory]
    [InlineData("alice", "email")]
    [InlineData("BOB", "sms")]
    [InlineData("chloe", "push")]
    public async Task ReturnsSeededProfiles(string userId, string channel)
    {
        var profile = await _provider.GetProfileAsync(userId, TestContext.Current.CancellationToken);

        Assert.NotNull(profile);
        Assert.Equal(channel, profile.PreferredChannel);
        Assert.NotNull(profile.ContactFor(channel));
        Assert.False(string.IsNullOrWhiteSpace(profile.FallbackTrack));
    }

    [Fact]
    public async Task SomeProfilesDoNotCoverEveryWeather()
    {
        var bob = await _provider.GetProfileAsync("bob", TestContext.Current.CancellationToken);

        Assert.False(bob!.TracksByWeather.ContainsKey(Weather.Neige));
    }

    [Fact]
    public async Task ReturnsNull_ForUnknownUser() =>
        Assert.Null(await _provider.GetProfileAsync("nobody", TestContext.Current.CancellationToken));
}
