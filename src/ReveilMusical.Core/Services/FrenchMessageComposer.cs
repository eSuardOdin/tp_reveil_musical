using System.Globalization;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Core.Services;

/// <summary>Rédige le message de réveil en français, selon le jour et la météo.</summary>
public sealed class FrenchMessageComposer : IMessageComposer
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public WakeUpMessage Compose(WakeUpRequest request, Track track)
    {
        var day = French.DateTimeFormat.GetDayName(request.Day);
        var title = $"Bon {day} !";
        var body = $"{DescribeWeather(request.Weather)} Pour bien commencer la journée : {track}.";
        if (track.ListenUrl is not null)
        {
            body += $" Écouter : {track.ListenUrl}";
        }

        return new WakeUpMessage(title, body, track);
    }

    internal static string DescribeWeather(Weather weather) => weather switch
    {
        Weather.Soleil => "Le soleil est au rendez-vous.",
        Weather.Pluie => "Il pleut aujourd'hui, pensez au parapluie.",
        Weather.Neige => "Il neige ! Couvrez-vous bien.",
        Weather.Nuageux => "Le ciel est nuageux.",
        _ => "Voici la météo du jour.",
    };
}
