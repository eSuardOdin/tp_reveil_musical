namespace ReveilMusical.Infrastructure.Music.MusicBrainz;

public sealed class MusicBrainzOptions
{
    public const string SectionName = "Music:MusicBrainz";
    public const string HttpClientName = "musicbrainz";

    public Uri BaseUrl { get; set; } = new("https://musicbrainz.org/ws/2/");
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
    public int Limit { get; set; } = 5;

    /// <summary>MusicBrainz exige un User-Agent identifiable : nom d'application…</summary>
    public string ApplicationName { get; set; } = "ReveilMusical";

    public string ApplicationVersion { get; set; } = "1.0";

    /// <summary>… et un contact (email ou URL).</summary>
    public string Contact { get; set; } = string.Empty;

    /// <summary>MusicBrainz limite à ~1 requête/seconde par client.</summary>
    public int PermitLimit { get; set; } = 1;
    public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(1);

    public string UserAgent => $"{ApplicationName}/{ApplicationVersion} ( {Contact} )";
}
