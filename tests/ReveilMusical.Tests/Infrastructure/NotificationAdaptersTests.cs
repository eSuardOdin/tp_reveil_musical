using NSubstitute;
using ReveilMusical.Core.Domain;
using ReveilMusical.Infrastructure.Notifications;
using ReveilMusical.Infrastructure.Notifications.Adapters;
using ReveilMusical.Infrastructure.Notifications.Vendors;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Infrastructure;

public class NotificationAdaptersTests
{
    private static WakeUpNotification Notification(string recipient, WakeUpMessage? message = null) =>
        new("u1", recipient, message ?? TestData.Message());

    [Fact]
    public async Task Email_SendsSubjectAndEncodedHtml()
    {
        var sender = Substitute.For<IMailSender>();
        var message = new WakeUpMessage("Bon lundi !", "<b>Salut</b>", TestData.SunTrack);

        await new EmailChannelAdapter(sender).SendAsync(Notification("a@x.com", message), TestContext.Current.CancellationToken);

        sender.Received(1).SendMail("a@x.com", "Bon lundi !", "<p>&lt;b&gt;Salut&lt;/b&gt;</p>");
    }

    [Fact]
    public async Task Email_PropagatesSdkFailure()
    {
        var sender = Substitute.For<IMailSender>();
        sender.When(s => s.SendMail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())).Throw(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new EmailChannelAdapter(sender).SendAsync(Notification("a@x.com"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Sms_SendsShortTextToPhone()
    {
        var gateway = Substitute.For<ISmsGateway>();
        gateway.SubmitAsync(Arg.Any<SmsPayload>(), Arg.Any<CancellationToken>()).Returns(202);

        await new SmsChannelAdapter(gateway).SendAsync(Notification("+336"), TestContext.Current.CancellationToken);

        await gateway.Received(1).SubmitAsync(new SmsPayload("+336", "Bon lundi ! Here Comes the Sun — The Beatles"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(503)]
    [InlineData(413)]
    [InlineData(100)]
    public async Task Sms_TurnsErrorCodeIntoException(int status)
    {
        var gateway = Substitute.For<ISmsGateway>();
        gateway.SubmitAsync(Arg.Any<SmsPayload>(), Arg.Any<CancellationToken>()).Returns(status);

        var ex = await Assert.ThrowsAsync<NotificationFailedException>(() =>
            new SmsChannelAdapter(gateway).SendAsync(Notification("+336"), TestContext.Current.CancellationToken));

        Assert.Equal("sms", ex.Channel);
        Assert.Contains(status.ToString(), ex.Message);
    }

    [Fact]
    public void Sms_TruncatesLongText()
    {
        var text = SmsChannelAdapter.BuildText(TestData.Message(new Track(new string('a', 300), "artist")));

        Assert.Equal(ISmsGateway.MaxLength, text.Length);
        Assert.EndsWith("…", text);
    }

    [Fact]
    public async Task Push_SendsPayloadWithTrackData()
    {
        var push = Substitute.For<IPushService>();
        push.Notify(Arg.Any<string>(), Arg.Any<PushPayload>()).Returns(new PushReceipt(true, null));

        await new PushChannelAdapter(push).SendAsync(Notification("token"), TestContext.Current.CancellationToken);

        push.Received(1).Notify("token", Arg.Is<PushPayload>(p =>
            p.Headline == "Bon lundi !" && p.Data["title"] == "Here Comes the Sun" && p.Data["url"] == "https://example.com/sun"));
    }

    [Fact]
    public async Task Push_OmitsUrl_WhenTrackHasNone()
    {
        var push = Substitute.For<IPushService>();
        push.Notify(Arg.Any<string>(), Arg.Any<PushPayload>()).Returns(new PushReceipt(true, null));

        await new PushChannelAdapter(push).SendAsync(Notification("token", TestData.Message(new Track("t", "a"))), TestContext.Current.CancellationToken);

        push.Received(1).Notify("token", Arg.Is<PushPayload>(p => !p.Data.ContainsKey("url")));
    }

    [Theory]
    [InlineData("quota", "quota")]
    [InlineData(null, "refusé")]
    public async Task Push_TurnsRejectedReceiptIntoException(string? error, string expected)
    {
        var push = Substitute.For<IPushService>();
        push.Notify(Arg.Any<string>(), Arg.Any<PushPayload>()).Returns(new PushReceipt(false, error));

        var ex = await Assert.ThrowsAsync<NotificationFailedException>(() =>
            new PushChannelAdapter(push).SendAsync(Notification("token"), TestContext.Current.CancellationToken));

        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public void Adapters_ExposeTheirKeys()
    {
        Assert.Equal("email", new EmailChannelAdapter(Substitute.For<IMailSender>()).Key);
        Assert.Equal("sms", new SmsChannelAdapter(Substitute.For<ISmsGateway>()).Key);
        Assert.Equal("push", new PushChannelAdapter(Substitute.For<IPushService>()).Key);
    }
}
