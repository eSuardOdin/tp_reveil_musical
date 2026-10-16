using System.Text.Json.Serialization;

namespace ReveilMusical.Infrastructure.Music.MusicBrainz;

// DTO propres à MusicBrainz : internes à l'adaptateur.
internal sealed record MusicBrainzSearchResponse(
    [property: JsonPropertyName("recordings")] IReadOnlyList<MusicBrainzRecording>? Recordings);

internal sealed record MusicBrainzRecording(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("artist-credit")] IReadOnlyList<MusicBrainzArtistCredit>? ArtistCredit);

internal sealed record MusicBrainzArtistCredit(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("joinphrase")] string? JoinPhrase);
