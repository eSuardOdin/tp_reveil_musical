using System.Text.Json.Serialization;

namespace ReveilMusical.Infrastructure.Music.Itunes;

// DTO propres à iTunes : internes à l'adaptateur, ils ne sortent jamais d'ici.
internal sealed record ItunesSearchResponse(
    [property: JsonPropertyName("resultCount")] int ResultCount,
    [property: JsonPropertyName("results")] IReadOnlyList<ItunesTrack>? Results);

internal sealed record ItunesTrack(
    [property: JsonPropertyName("trackName")] string? TrackName,
    [property: JsonPropertyName("artistName")] string? ArtistName,
    [property: JsonPropertyName("trackViewUrl")] string? TrackViewUrl);
