using System.Net;
using Microsoft.Extensions.Options;
using NSubstitute;
using ReveilMusical.Infrastructure.Music;
using ReveilMusical.Infrastructure.Music.Itunes;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Infrastructure;

public class ItunesMusicProviderTests
{
    private static ItunesMusicProvider Provider(StubHttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(ItunesOptions.HttpClientName)
            .Returns(_ => new HttpClient(handler) { BaseAddress = new Uri("https://itunes.test/") });
        return new ItunesMusicProvider(factory, Options.Create(new ItunesOptions { Limit = 3 }));
    }

    [Fact]
    public async Task MapsFirstResult_ToDomainTrack()
    {
        var handler = StubHttpMessageHandler.Json("""
            {"resultCount":2,"results":[
              {"trackName":" Riders on the Storm ","artistName":"The Doors","trackViewUrl":"https://music.apple.com/x","collectionName":"L.A. Woman"},
              {"trackName":"Other","artistName":"Other"}]}
            """);

        var track = await Provider(handler).FindTrackAsync("riders storm", TestContext.Current.CancellationToken);

        Assert.NotNull(track);
        Assert.Equal("Riders on the Storm", track.Title);
        Assert.Equal("The Doors", track.Artist);
        Assert.Equal(new Uri("https://music.apple.com/x"), track.ListenUrl);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://itunes.test/search?term=riders%20storm&media=music&entity=song&limit=3", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task HandlesMissingArtistAndInvalidUrl()
    {
        var handler = StubHttpMessageHandler.Json("""{"resultCount":1,"results":[{"trackName":"Song","trackViewUrl":"not a url"}]}""");

        var track = await Provider(handler).FindTrackAsync("song", TestContext.Current.CancellationToken);

        Assert.Equal("Artiste inconnu", track!.Artist);
        Assert.Null(track.ListenUrl);
    }

    [Theory]
    [InlineData("""{"resultCount":0,"results":[]}""")]
    [InlineData("""{"resultCount":0}""")]
    [InlineData("""{"resultCount":1,"results":[{"artistName":"No title"}]}""")]
    public async Task ReturnsNull_WhenNoUsableResult(string json) =>
        Assert.Null(await Provider(StubHttpMessageHandler.Json(json)).FindTrackAsync("x", TestContext.Current.CancellationToken));

    [Fact]
    public async Task Throws_OnHttpError() =>
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Provider(StubHttpMessageHandler.Json("{}", HttpStatusCode.ServiceUnavailable)).FindTrackAsync("x", TestContext.Current.CancellationToken));

    [Fact]
    public async Task Throws_OnNullBody() =>
        await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            Provider(StubHttpMessageHandler.Json("null")).FindTrackAsync("x", TestContext.Current.CancellationToken));

    [Fact]
    public void Name_IsItunes() => Assert.Equal("iTunes", Provider(StubHttpMessageHandler.Json("{}")).Name);
}
