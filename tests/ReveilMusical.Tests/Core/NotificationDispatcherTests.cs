using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Core.Services;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Core;

public class NotificationDispatcherTests
{
    private static INotificationChannel Channel(string key, bool fails = false)
    {
        var channel = Substitute.For<INotificationChannel>();
        channel.Key.Returns(key);
        if (fails)
        {
            channel.SendAsync(Arg.Any<WakeUpNotification>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("down"));
        }

        return channel;
    }

    private static NotificationDispatcher Dispatcher(params INotificationChannel[] channels) =>
        new(channels, NullLogger<NotificationDispatcher>.Instance);

    [Fact]
    public async Task SendsOnPreferredChannel_EvenIfRegisteredLast()
    {
        var email = Channel("email");
        var sms = Channel("sms");
        var message = TestData.Message();

        var result = await Dispatcher(sms, email).DispatchAsync(TestData.Profile("email"), message, TestContext.Current.CancellationToken);

        Assert.Equal(new DeliveryResult("email", true), result);
        await email.Received(1).SendAsync(new WakeUpNotification("u1", "u1@example.com", message), Arg.Any<CancellationToken>());
        await sms.DidNotReceive().SendAsync(Arg.Any<WakeUpNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreferredChannelMatching_IsCaseInsensitive()
    {
        var result = await Dispatcher(Channel("sms"), Channel("email"))
            .DispatchAsync(TestData.Profile("EMAIL"), TestData.Message(), TestContext.Current.CancellationToken);

        Assert.Equal(new DeliveryResult("email", true), result);
    }

    [Fact]
    public async Task FallsBackToAnotherChannel_WhenPreferredFails()
    {
        var sms = Channel("sms");

        var result = await Dispatcher(Channel("email", fails: true), sms)
            .DispatchAsync(TestData.Profile("email"), TestData.Message(), TestContext.Current.CancellationToken);

        Assert.Equal(new DeliveryResult("sms", false), result);
        await sms.Received(1).SendAsync(Arg.Is<WakeUpNotification>(n => n.Recipient == "+33600000000"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsChannelsWithoutContact()
    {
        var push = Channel("push");

        var result = await Dispatcher(push, Channel("email"))
            .DispatchAsync(TestData.Profile("push"), TestData.Message(), TestContext.Current.CancellationToken);

        Assert.Equal(new DeliveryResult("email", false), result);
        await push.DidNotReceive().SendAsync(Arg.Any<WakeUpNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallsBack_WhenPreferredChannelIsNotRegistered()
    {
        var result = await Dispatcher(Channel("email"))
            .DispatchAsync(TestData.Profile("whatsapp"), TestData.Message(), TestContext.Current.CancellationToken);

        Assert.Equal(new DeliveryResult("email", false), result);
    }

    [Fact]
    public async Task ReportsNotDelivered_WhenEveryChannelFails()
    {
        var result = await Dispatcher(Channel("email", fails: true), Channel("sms", fails: true))
            .DispatchAsync(TestData.Profile(), TestData.Message(), TestContext.Current.CancellationToken);

        Assert.False(result.Delivered);
    }
}
