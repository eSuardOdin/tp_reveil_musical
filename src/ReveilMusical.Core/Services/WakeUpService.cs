using Microsoft.Extensions.Logging;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Core.Services;

/// <summary>
/// Orchestration du réveil : préférences → choix du morceau → recherche → message → envoi.
/// Ne connaît que des abstractions : aucun fournisseur ni canal concret.
/// </summary>
public sealed class WakeUpService(
    IUserProfileProvider profiles,
    ITrackSelector selector,
    IMusicResolver music,
    IMessageComposer composer,
    INotificationDispatcher dispatcher,
    ILogger<WakeUpService> logger) : IWakeUpService
{
    public async Task<WakeUpResult> SendWakeUpAsync(WakeUpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        var profile = await profiles.GetProfileAsync(request.UserId, cancellationToken)
                      ?? throw new UnknownUserException(request.UserId);

        var query = selector.SelectQuery(profile, request);
        var lookup = await music.ResolveAsync(query, request, cancellationToken);
        var message = composer.Compose(request, lookup.Track);
        var delivery = await dispatcher.DispatchAsync(profile, message, cancellationToken);

        var degraded = lookup.IsFallback || !delivery.UsedPreferredChannel;
        logger.LogInformation(
            "Réveil de {UserId} : {Track} (source {Source}) via {Channel}{Degraded}.",
            request.UserId, lookup.Track, lookup.Source, delivery.Channel ?? "aucun canal", degraded ? " [mode dégradé]" : string.Empty);

        return new WakeUpResult(lookup.Track, lookup.Source, delivery.Channel, degraded);
    }
}
