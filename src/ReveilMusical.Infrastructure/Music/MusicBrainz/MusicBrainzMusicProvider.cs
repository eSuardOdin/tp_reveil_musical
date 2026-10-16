using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Infrastructure.Music.MusicBrainz;

/// <summary>
/// Adaptateur MusicBrainz. L'en-tête User-Agent obligatoire est posé lors de la
/// configuration du client HTTP nommé (voir <c>ServiceCollectionExtensions</c>).
/// </summary>
public sealed class MusicBrainzMusicProvider(IHttpClientFactory httpClientFactory, IOptions<MusicBrainzOptions> options) : IMusicProvider
{
    public string Name => "MusicBrainz";

    public async Task<Track?> FindTrackAsync(string query, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(MusicBrainzOptions.HttpClientName);
        var uri = $"recording?query={Uri.EscapeDataString(query)}&fmt=json&limit={options.Value.Limit}";

        var response = await client.GetFromJsonAsync<MusicBrainzSearchResponse>(uri, cancellationToken)
                       ?? throw new ProviderUnavailableException(Name, "réponse vide");

        var match = response.Recordings?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Title));
        return match is null ? null : ToTrack(match);
    }

    private static Track ToTrack(MusicBrainzRecording dto)
    {
        var artist = dto.ArtistCredit is { Count: > 0 } credits
            ? string.Concat(credits.Select(c => $"{c.Name}{c.JoinPhrase}")).Trim()
            : string.Empty;

        Uri? url = string.IsNullOrWhiteSpace(dto.Id) ? null : new Uri($"https://musicbrainz.org/recording/{dto.Id}");

        return new Track(dto.Title!.Trim(), artist.Length == 0 ? "Artiste inconnu" : artist, url);
    }
}
