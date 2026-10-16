using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Core.Services;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Core;

public class MusicResolverTests
{
    private static readonly Track LocalTrack = new("Local", "Fallback");
    private readonly IFallbackMusicProvider _fallback = Substitute.For<IFallbackMusicProvider>();

    public MusicResolverTests()
    {
        _fallback.Name.Returns("local");
        _fallback.GetTrack(Arg.Any<WakeUpRequest>()).Returns(LocalTrack);
    }

    private static IMusicProvider Provider(string name, Track? result = null, Exception? error = null)
    {
        var provider = Substitute.For<IMusicProvider>();
        provider.Name.Returns(name);
        if (error is not null)
        {
            provider.FindTrackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(error);
        }
        else
        {
            provider.FindTrackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
        }

        return provider;
    }

    private MusicResolver Resolver(params IMusicProvider[] providers) =>
        new(providers, _fallback, NullLogger<MusicResolver>.Instance);

    [Fact]
    public async Task UsesFirstProvider_WhenItFindsATrack()
    {
        var second = Provider("second", TestData.SunTrack);
        var result = await Resolver(Provider("first", TestData.SunTrack), second).ResolveAsync("sun", TestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(new MusicLookupResult(TestData.SunTrack, "first", false), result);
        await second.DidNotReceive().FindTrackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SwitchesToNextProvider_WhenFirstFails()
    {
        var result = await Resolver(Provider("down", error: new HttpRequestException("boom")), Provider("up", TestData.SunTrack))
            .ResolveAsync("sun", TestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal("up", result.Source);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public async Task SwitchesToNextProvider_WhenFirstFindsNothing()
    {
        var result = await Resolver(Provider("empty"), Provider("up", TestData.SunTrack))
            .ResolveAsync("sun", TestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal("up", result.Source);
    }

    [Fact]
    public async Task UsesLocalFallback_WhenAllProvidersFail()
    {
        var request = TestData.Request();
        var result = await Resolver(Provider("a", error: new TimeoutException()), Provider("b"))
            .ResolveAsync("sun", request, TestContext.Current.CancellationToken);

        Assert.Equal(new MusicLookupResult(LocalTrack, "local", true), result);
        _fallback.Received(1).GetTrack(request);
    }

    [Fact]
    public async Task UsesLocalFallback_WhenNoProviderConfigured()
    {
        var result = await Resolver().ResolveAsync("sun", TestData.Request(), TestContext.Current.CancellationToken);

        Assert.True(result.IsFallback);
    }

    [Fact]
    public async Task PropagatesCancellation_FromCaller()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var resolver = Resolver(Provider("a", error: new OperationCanceledException(cts.Token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resolver.ResolveAsync("sun", TestData.Request(), cts.Token));
    }
}
