using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReveilMusical.Infrastructure.Notifications.Vendors;

namespace ReveilMusical.Tests.Infrastructure;

public class FakeVendorsTests
{
    private static IOptions<MockOutageOptions> Outages(params string[] down) =>
        Options.Create(new MockOutageOptions { Outages = [.. down] });

    [Fact]
    public void Mail_Succeeds_OrThrowsDuringOutage()
    {
        new FakeMailSender(NullLogger<FakeMailSender>.Instance, Outages()).SendMail("a@x", "s", "b");

        Assert.Throws<InvalidOperationException>(() =>
            new FakeMailSender(NullLogger<FakeMailSender>.Instance, Outages("EMAIL")).SendMail("a@x", "s", "b"));
    }

    [Fact]
    public async Task Sms_ReturnsStatusCodes()
    {
        var ok = new FakeSmsGateway(NullLogger<FakeSmsGateway>.Instance, Outages());
        var down = new FakeSmsGateway(NullLogger<FakeSmsGateway>.Instance, Outages("sms"));
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal(202, await ok.SubmitAsync(new SmsPayload("+33", "hello"), ct));
        Assert.Equal(413, await ok.SubmitAsync(new SmsPayload("+33", new string('x', 161)), ct));
        Assert.Equal(503, await down.SubmitAsync(new SmsPayload("+33", "hello"), ct));
    }

    [Fact]
    public void Push_ReturnsReceipts()
    {
        var payload = new PushPayload("h", "c", new Dictionary<string, string> { ["k"] = "v" });

        Assert.True(new FakePushService(NullLogger<FakePushService>.Instance, Outages()).Notify("t", payload).Accepted);
        Assert.False(new FakePushService(NullLogger<FakePushService>.Instance, Outages("push")).Notify("t", payload).Accepted);
    }
}
