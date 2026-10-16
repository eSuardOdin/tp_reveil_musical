using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;

namespace ReveilMusical.Infrastructure.Music.Itunes;

/// <summary>Adaptateur iTunes Search API : traduit la réponse iTunes en <see cref="Track"/>.</summary>
public sealed class ItunesMusicProvider(IHttpClientFactory httpClientFactory, IOptions<ItunesOptions> options) : IMusicProvider
{
    public string Name => "iTunes";

    public async Task<Track?> FindTrackAsync(string query, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(ItunesOptions.HttpClientName);
        var uri = $"search?term={Uri.EscapeDataString(query)}&media=music&entity=song&limit={options.Value.Limit}";

        var response = await client.GetFromJsonAsync<ItunesSearchResponse>(uri, cancellationToken)
                       ?? throw new ProviderUnavailableException(Name, "réponse vide");

        var match = response.Results?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.TrackName));
        return match is null ? null : ToTrack(match);
    }

    private static Track ToTrack(ItunesTrack dto) => new(
        dto.TrackName!.Trim(),
        string.IsNullOrWhiteSpace(dto.ArtistName) ? "Artiste inconnu" : dto.ArtistName.Trim(),
        Uri.TryCreate(dto.TrackViewUrl, UriKind.Absolute, out var url) ? url : null);
}
