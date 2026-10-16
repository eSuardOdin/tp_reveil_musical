using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReveilMusical.Infrastructure.Notifications.Vendors;

/// <summary>Charge utile du « SDK » push simulé.</summary>
public sealed record PushPayload(string Headline, string Content, IReadOnlyDictionary<string, string> Data);

/// <summary>Accusé du « SDK » push simulé.</summary>
public sealed record PushReceipt(bool Accepted, string? Error);

/// <summary>« SDK » push simulé : jeton d'appareil + payload, retourne un accusé.</summary>
public interface IPushService
{
    PushReceipt Notify(string deviceToken, PushPayload payload);
}

public sealed class FakePushService(ILogger<FakePushService> logger, IOptions<MockOutageOptions> outages) : IPushService
{
    public PushReceipt Notify(string deviceToken, PushPayload payload)
    {
        if (outages.Value.IsDown("push"))
        {
            return new PushReceipt(false, "service push indisponible (panne simulée)");
        }

        logger.LogInformation("[MOCK PUSH] Appareil: {Token} | {Headline} | {Content} | {Data}",
            deviceToken, payload.Headline, payload.Content, string.Join(", ", payload.Data.Select(kv => $"{kv.Key}={kv.Value}")));
        return new PushReceipt(true, null);
    }
}
