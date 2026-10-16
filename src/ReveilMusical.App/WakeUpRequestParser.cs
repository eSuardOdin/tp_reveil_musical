using System.Globalization;
using ReveilMusical.Core.Domain;

namespace ReveilMusical.App;

/// <summary>Traduit les arguments de l'appel (ID, jour, météo) en <see cref="WakeUpRequest"/>.</summary>
public static class WakeUpRequestParser
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public static WakeUpRequest Parse(string userId, string day, string weather)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new WakeUpRequest(userId.Trim(), ParseDay(day), ParseWeather(weather));
    }

    public static DayOfWeek ParseDay(string value)
    {
        var trimmed = value.Trim();
        if (Enum.TryParse<DayOfWeek>(trimmed, ignoreCase: true, out var day) && Enum.IsDefined(day))
        {
            return day;
        }

        foreach (var candidate in Enum.GetValues<DayOfWeek>())
        {
            if (string.Equals(French.DateTimeFormat.GetDayName(candidate), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        throw new ArgumentException($"Jour de la semaine invalide : '{value}'.", nameof(value));
    }

    public static Weather ParseWeather(string value)
    {
        if (Enum.TryParse<Weather>(value.Trim(), ignoreCase: true, out var weather) && Enum.IsDefined(weather))
        {
            return weather;
        }

        throw new ArgumentException(
            $"Météo invalide : '{value}'. Valeurs possibles : {string.Join(" / ", Enum.GetNames<Weather>().Select(n => n.ToUpperInvariant()))}.",
            nameof(value));
    }
}
