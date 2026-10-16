using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReveilMusical.Infrastructure.Notifications.Vendors;

/// <summary>Charge utile du « SDK » SMS simulé.</summary>
public sealed record SmsPayload(string PhoneNumber, string Text);

/// <summary>« SDK » SMS simulé : API asynchrone qui renvoie un code de statut au lieu de lever une exception.</summary>
public interface ISmsGateway
{
    public const int MaxLength = 160;

    /// <returns>Code de statut façon HTTP (202 = accepté).</returns>
    Task<int> SubmitAsync(SmsPayload payload, CancellationToken cancellationToken = default);
}

public sealed class FakeSmsGateway(ILogger<FakeSmsGateway> logger, IOptions<MockOutageOptions> outages) : ISmsGateway
{
    public Task<int> SubmitAsync(SmsPayload payload, CancellationToken cancellationToken = default)
    {
        if (outages.Value.IsDown("sms"))
        {
            return Task.FromResult(503);
        }

        if (payload.Text.Length > ISmsGateway.MaxLength)
        {
            return Task.FromResult(413);
        }

        logger.LogInformation("[MOCK SMS] À: {Phone} | {Text}", payload.PhoneNumber, payload.Text);
        return Task.FromResult(202);
    }
}
