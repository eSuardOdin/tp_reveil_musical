using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReveilMusical.Infrastructure.Notifications.Vendors;

/// <summary>« SDK » email simulé : API synchrone, sujet + corps HTML.</summary>
public interface IMailSender
{
    void SendMail(string toAddress, string subject, string htmlBody);
}

public sealed class FakeMailSender(ILogger<FakeMailSender> logger, IOptions<MockOutageOptions> outages) : IMailSender
{
    public void SendMail(string toAddress, string subject, string htmlBody)
    {
        if (outages.Value.IsDown("email"))
        {
            throw new InvalidOperationException("SMTP injoignable (panne simulée).");
        }

        logger.LogInformation("[MOCK EMAIL] À: {To} | Sujet: {Subject} | {Body}", toAddress, subject, htmlBody);
    }
}
