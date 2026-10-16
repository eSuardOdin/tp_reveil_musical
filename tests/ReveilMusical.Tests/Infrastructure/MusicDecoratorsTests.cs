using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Infrastructure.Music;
using ReveilMusical.Infrastructure.Music.Decorators;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Infrastructure;

public class MusicDecoratorsTests
{
    private readonly IMusicProvider _inner = Substitute.For<IMusicProvider>();

    public MusicDecoratorsTests()
    {
        _inner.Name.Returns("inner");
        _inner.FindTrackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(TestData.SunTrack);
    }

    [Fact]
    public async Task RateLimit_AllowsCallsWithinQuota_ThenRejects()
    {
        var time = new FakeTimeProvider();
        var provider = new RateLimitedMusicProvider(_inner, new RateLimitOptions(2, TimeSpan.FromMinutes(1)), time);
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal(TestData.SunTrack, await provider.FindTrackAsync("a", ct));
        Assert.Equal(TestData.SunTrack, await provider.FindTrackAsync("b", ct));
        var ex = await Assert.ThrowsAsync<ProviderUnavailableException>(() => provider.FindTrackAsync("c", ct));

        Assert.Equal("inner", ex.Provider);
        Assert.Equal("inner", provider.Name);
        await _inner.Received(2).FindTrackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RateLimit_FreesPermits_WhenWindowSlides()
    {
        var time = new FakeTimeProvider();
        var provider = new RateLimitedMusicProvider(_inner, new RateLimitOptions(1, TimeSpan.FromSeconds(1)), time);
        var ct = TestContext.Current.CancellationToken;

        await provider.FindTrackAsync("a", ct);
        await Assert.ThrowsAsync<ProviderUnavailableException>(() => provider.FindTrackAsync("b", ct));
        time.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(TestData.SunTrack, await provider.FindTrackAsync("c", ct));
    }

    private static MemoryCache NewCache() => new(Options.Create(new MemoryCacheOptions()));

    [Fact]
    public async Task Cache_CallsInnerOnce_ForSameNormalizedQuery()
    {
        using var cache = NewCache();
        var provider = new CachingMusicProvider(_inner, cache, TimeSpan.FromHours(1));
        var ct = TestContext.Current.CancellationToken;

        var first = await provider.FindTrackAsync("Here Comes The Sun", ct);
        var second = await provider.FindTrackAsync("  here comes the sun ", ct);

        Assert.Equal(first, second);
        Assert.Equal("inner", provider.Name);
        await _inner.Received(1).FindTrackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cache_RemembersEmptyResults()
    {
        _inner.FindTrackAsync("nothing", Arg.Any<CancellationToken>()).Returns((Track?)null);
        using var cache = NewCache();
        var provider = new CachingMusicProvider(_inner, cache, TimeSpan.FromHours(1));
        var ct = TestContext.Current.CancellationToken;

        Assert.Null(await provider.FindTrackAsync("nothing", ct));
        Assert.Null(await provider.FindTrackAsync("nothing", ct));
        await _inner.Received(1).FindTrackAsync("nothing", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cache_DoesNotRememberFailures()
    {
        var calls = 0;
        _inner.FindTrackAsync("flaky", Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 1 ? throw new HttpRequestException("down") : Task.FromResult<Track?>(TestData.SunTrack));
        using var cache = NewCache();
        var provider = new CachingMusicProvider(_inner, cache, TimeSpan.FromHours(1));
        var ct = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.FindTrackAsync("flaky", ct));
        Assert.Equal(TestData.SunTrack, await provider.FindTrackAsync("flaky", ct));
    }

    [Fact]
    public async Task Cache_HitsDoNotConsumeRateLimit()
    {
        using var cache = NewCache();
        var limited = new RateLimitedMusicProvider(_inner, new RateLimitOptions(1, TimeSpan.FromMinutes(1)), new FakeTimeProvider());
        var provider = new CachingMusicProvider(limited, cache, TimeSpan.FromHours(1));
        var ct = TestContext.Current.CancellationToken;

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(TestData.SunTrack, await provider.FindTrackAsync("same", ct));
        }
    }
}
