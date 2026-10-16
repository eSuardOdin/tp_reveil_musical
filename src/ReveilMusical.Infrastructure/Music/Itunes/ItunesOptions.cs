namespace ReveilMusical.Infrastructure.Music.Itunes;

public sealed class ItunesOptions
{
    public const string SectionName = "Music:Itunes";
    public const string HttpClientName = "itunes";

    public Uri BaseUrl { get; set; } = new("https://itunes.apple.com/");
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
    public int Limit { get; set; } = 5;

    /// <summary>iTunes tolère ~20 requêtes/minute.</summary>
    public int PermitLimit { get; set; } = 20;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
