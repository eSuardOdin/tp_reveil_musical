using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReveilMusical.Core.Domain;
using ReveilMusical.Core.Ports;
using ReveilMusical.Infrastructure.DependencyInjection;
using ReveilMusical.Infrastructure.Music.Itunes;
using ReveilMusical.Infrastructure.Music.MusicBrainz;
using ReveilMusical.Tests.Helpers;

namespace ReveilMusical.Tests.Infrastructure;

public class ServiceCollectionExtensionsTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> DefaultValues(params string[] providers)
    {
        var values = new Dictionary<string, string?> { ["Music:MusicBrainz:Contact"] = "test@example.com" };
        for (var i = 0; i < providers.Length; i++)
        {
            values[$"Music:Providers:{i}"] = providers[i];
        }

        return values;
    }

    private static ServiceProvider Build(Dictionary<string, string?> values, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddReveilMusical(Config(values));
        configure?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void RegistersProviders_InConfiguredOrder()
    {
        using var sp = Build(DefaultValues("musicbrainz", "ITUNES"));

        var names = sp.GetServices<IMusicProvider>().Select(p => p.Name);

        Assert.Equal(["MusicBrainz", "iTunes"], names);
    }

    [Fact]
    public void RegistersNoRemoteProvider_WhenListIsEmpty()
    {
        using var sp = Build(DefaultValues());

        Assert.Empty(sp.GetServices<IMusicProvider>());
        Assert.NotNull(sp.GetRequiredService<IFallbackMusicProvider>());
    }

    [Fact]
    public void Throws_OnUnknownProvider()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(DefaultValues("spotify")));

        Assert.Contains("spotify", ex.Message);
    }

    [Fact]
    public void RegistersAllNotificationChannels()
    {
        using var sp = Build(DefaultValues());

        Assert.Equal(["email", "sms", "push"], sp.GetServices<INotificationChannel>().Select(c => c.Key));
    }

    [Fact]
    public void RejectsMusicBrainzConfig_WithoutContact()
    {
        using var sp = Build(new Dictionary<string, string?>());

        Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<MusicBrainzOptions>>().Value);
    }

    [Fact]
    public void ConfiguresHttpClients_FromOptions()
    {
        var values = DefaultValues();
        values["Music:Itunes:BaseUrl"] = "https://itunes.test/";
        values["Music:Itunes:Timeout"] = "00:00:02";
        using var sp = Build(values);
        var factory = sp.GetRequiredService<IHttpClientFactory>();

        var itunes = factory.CreateClient(ItunesOptions.HttpClientName);
        var musicBrainz = factory.CreateClient(MusicBrainzOptions.HttpClientName);

        Assert.Equal(new Uri("https://itunes.test/"), itunes.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(2), itunes.Timeout);
        Assert.Equal("ReveilMusical/1.0 ( test@example.com )", musicBrainz.DefaultRequestHeaders.UserAgent.ToString());
    }

    [Fact]
    public async Task EndToEnd_FallsBackFromItunesToMusicBrainz_ThenFromSmsToEmail()
    {
        var itunes = StubHttpMessageHandler.Json("{}", HttpStatusCode.InternalServerError);
        var musicBrainz = StubHttpMessageHandler.Json("""{"recordings":[{"id":"1","title":"Purple Rain","artist-credit":[{"name":"Prince"}]}]}""");
        var values = DefaultValues("itunes", "musicbrainz");
        values["Notifications:Mocks:Outages:0"] = "sms";

        using var sp = Build(values, services =>
        {
            services.AddHttpClient(ItunesOptions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => itunes);
            services.AddHttpClient(MusicBrainzOptions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => musicBrainz);
        });

        var result = await sp.GetRequiredService<IWakeUpService>()
            .SendWakeUpAsync(new WakeUpRequest("bob", DayOfWeek.Friday, Weather.Pluie), TestContext.Current.CancellationToken);

        Assert.Equal(new Track("Purple Rain", "Prince", new Uri("https://musicbrainz.org/recording/1")), result.Track);
        Assert.Equal("MusicBrainz", result.MusicSource);
        Assert.Equal("email", result.Channel);
        Assert.True(result.IsDegraded);
        Assert.Contains("ReveilMusical", musicBrainz.Requests[0].Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task EndToEnd_NeverSilent_WhenEveryProviderIsDown()
    {
        var down = new StubHttpMessageHandler(_ => throw new HttpRequestException("réseau coupé"));
        using var sp = Build(DefaultValues("itunes", "musicbrainz"), services =>
        {
            services.AddHttpClient(ItunesOptions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => down);
            services.AddHttpClient(MusicBrainzOptions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => down);
        });

        var result = await sp.GetRequiredService<IWakeUpService>()
            .SendWakeUpAsync(new WakeUpRequest("alice", DayOfWeek.Sunday, Weather.Neige), TestContext.Current.CancellationToken);

        Assert.Equal("Secours local", result.MusicSource);
        Assert.Equal("email", result.Channel);
        Assert.True(result.IsDegraded);
    }
}
