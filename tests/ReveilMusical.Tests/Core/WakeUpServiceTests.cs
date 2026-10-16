using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Core.Services;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Core;

public class WakeUpServiceTests
{
    private readonly IUserProfileProvider _profiles = Substitute.For<IUserProfileProvider>();
    private readonly ITrackSelector _selector = Substitute.For<ITrackSelector>();
    private readonly IMusicResolver _music = Substitute.For<IMusicResolver>();
    private readonly IMessageComposer _composer = Substitute.For<IMessageComposer>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly WakeUpService _service;

    private readonly UserProfile _profile = TestData.Profile();
    private readonly WakeUpMessage _message = TestData.Message();

    public WakeUpServiceTests()
    {
        _service = new WakeUpService(_profiles, _selector, _music, _composer, _dispatcher, NullLogger<WakeUpService>.Instance);
        _profiles.GetProfileAsync("u1", Arg.Any<CancellationToken>()).Returns(_profile);
        _selector.SelectQuery(_profile, Arg.Any<WakeUpRequest>()).Returns("sun");
        _composer.Compose(Arg.Any<WakeUpRequest>(), TestData.SunTrack).Returns(_message);
    }

    private void MusicReturns(bool isFallback) =>
        _music.ResolveAsync("sun", Arg.Any<WakeUpRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MusicLookupResult(TestData.SunTrack, "iTunes", isFallback));

    private void DeliveryReturns(string? channel, bool preferred) =>
        _dispatcher.DispatchAsync(_profile, _message, Arg.Any<CancellationToken>()).Returns(new DeliveryResult(channel, preferred));

    [Fact]
    public async Task NominalCase_IsNotDegraded()
    {
        MusicReturns(isFallback: false);
        DeliveryReturns("email", preferred: true);

        var result = await _service.SendWakeUpAsync(TestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(new WakeUpResult(TestData.SunTrack, "iTunes", "email", false), result);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task IsDegraded_WhenMusicFallbackOrOtherChannel(bool musicFallback, bool preferredChannel)
    {
        MusicReturns(musicFallback);
        DeliveryReturns("sms", preferredChannel);

        var result = await _service.SendWakeUpAsync(TestData.Request(), TestContext.Current.CancellationToken);

        Assert.True(result.IsDegraded);
        Assert.True(result.Delivered);
    }

    [Fact]
    public async Task ReportsUndelivered_WhenNoChannelWorks()
    {
        MusicReturns(isFallback: false);
        DeliveryReturns(null, preferred: false);

        var result = await _service.SendWakeUpAsync(TestData.Request(), TestContext.Current.CancellationToken);

        Assert.False(result.Delivered);
    }

    [Fact]
    public async Task Throws_WhenUserIsUnknown()
    {
        var ex = await Assert.ThrowsAsync<UnknownUserException>(
            () => _service.SendWakeUpAsync(TestData.Request(userId: "ghost"), TestContext.Current.CancellationToken));

        Assert.Equal("ghost", ex.UserId);
    }

    [Fact]
    public async Task Throws_WhenUserIdIsBlank() =>
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SendWakeUpAsync(TestData.Request(userId: " "), TestContext.Current.CancellationToken));
}
