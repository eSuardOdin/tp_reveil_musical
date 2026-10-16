using System.Net;
using Microsoft.Extensions.Options;
using NSubstitute;
using ReveilMusical.Infrastructure.Music;
using ReveilMusical.Infrastructure.Music.MusicBrainz;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Infrastructure;

public class MusicBrainzMusicProviderTests
{
    private static MusicBrainzMusicProvider Provider(StubHttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(MusicBrainzOptions.HttpClientName)
            .Returns(_ => new HttpClient(handler) { BaseAddress = new Uri("https://mb.test/ws/2/") });
        return new MusicBrainzMusicProvider(factory, Options.Create(new MusicBrainzOptions { Limit = 2, Contact = "c@x" }));
    }

    [Fact]
    public async Task MapsRecording_JoiningArtistCredits()
    {
        var handler = StubHttpMessageHandler.Json("""
            {"recordings":[{"id":"abc-123","title":"Under Pressure","artist-credit":[
              {"name":"Queen","joinphrase":" & "},{"name":"David Bowie"}]}]}
            """);

        var track = await Provider(handler).FindTrackAsync("under pressure", TestContext.Current.CancellationToken);

        Assert.NotNull(track);
        Assert.Equal("Under Pressure", track.Title);
        Assert.Equal("Queen & David Bowie", track.Artist);
        Assert.Equal(new Uri("https://musicbrainz.org/recording/abc-123"), track.ListenUrl);
        Assert.Equal("https://mb.test/ws/2/recording?query=under%20pressure&fmt=json&limit=2", handler.Requests[0].RequestUri!.AbsoluteUri);
    }

    [Theory]
    [InlineData("""{"recordings":[{"title":"Song"}]}""")]
    [InlineData("""{"recordings":[{"title":"Song","artist-credit":[]}]}""")]
    public async Task HandlesMissingArtistAndId(string json)
    {
        var track = await Provider(StubHttpMessageHandler.Json(json)).FindTrackAsync("song", TestContext.Current.CancellationToken);

        Assert.Equal("Artiste inconnu", track!.Artist);
        Assert.Null(track.ListenUrl);
    }

    [Theory]
    [InlineData("""{"recordings":[]}""")]
    [InlineData("""{}""")]
    public async Task ReturnsNull_WhenNoRecording(string json) =>
        Assert.Null(await Provider(StubHttpMessageHandler.Json(json)).FindTrackAsync("x", TestContext.Current.CancellationToken));

    [Fact]
    public async Task Throws_WhenRejected() =>
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Provider(StubHttpMessageHandler.Json("{}", HttpStatusCode.Forbidden)).FindTrackAsync("x", TestContext.Current.CancellationToken));

    [Fact]
    public async Task Throws_OnNullBody() =>
        await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            Provider(StubHttpMessageHandler.Json("null")).FindTrackAsync("x", TestContext.Current.CancellationToken));

    [Fact]
    public void Options_BuildIdentifiableUserAgent()
    {
        var options = new MusicBrainzOptions { ApplicationName = "App", ApplicationVersion = "2.0", Contact = "me@x.org" };

        Assert.Equal("App/2.0 ( me@x.org )", options.UserAgent);
        Assert.Equal("MusicBrainz", Provider(StubHttpMessageHandler.Json("{}")).Name);
    }
}
