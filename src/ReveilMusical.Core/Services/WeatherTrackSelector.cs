using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Core.Services;

/// <summary>
/// Choisit le morceau associé à la météo du jour, ou le morceau de secours
/// de l'utilisateur si cette météo n'est pas couverte.
/// </summary>
public sealed class WeatherTrackSelector : ITrackSelector
{
    public string SelectQuery(UserProfile profile, WakeUpRequest request)
    {
        if (profile.TracksByWeather.TryGetValue(request.Weather, out var track) && !string.IsNullOrWhiteSpace(track))
        {
            return track;
        }

        return profile.FallbackTrack;
    }
}
