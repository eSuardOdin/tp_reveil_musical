using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ReveilMusical.Core.Ports;
using ReveilMusical.Core.Services;
using ReveilMusical.Infrastructure.Music;
using ReveilMusical.Infrastructure.Music.Decorators;
using ReveilMusical.Infrastructure.Music.Itunes;
using ReveilMusical.Infrastructure.Music.Local;
using ReveilMusical.Infrastructure.Music.MusicBrainz;
using ReveilMusical.Infrastructure.Notifications.Adapters;
using ReveilMusical.Infrastructure.Notifications.Vendors;
using ReveilMusical.Infrastructure.Profiles;

namespace ReveilMusical.Infrastructure.DependencyInjection;

/// <summary>
/// Racine de composition : seul endroit qui connaît les implémentations concrètes.
/// Le conteneur les instancie ; aucune classe n'appelle <c>new</c> sur un service.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Fournisseurs musicaux connus, par clé de configuration.</summary>
    private static readonly IReadOnlyDictionary<string, Func<IServiceProvider, IMusicProvider>> MusicProviderFactories =
        new Dictionary<string, Func<IServiceProvider, IMusicProvider>>(StringComparer.OrdinalIgnoreCase)
        {
            ["itunes"] = sp =>
            {
                var options = sp.GetRequiredService<IOptions<ItunesOptions>>().Value;
                return Decorate(sp, sp.GetRequiredService<ItunesMusicProvider>(), new RateLimitOptions(options.PermitLimit, options.Window));
            },
            ["musicbrainz"] = sp =>
            {
                var options = sp.GetRequiredService<IOptions<MusicBrainzOptions>>().Value;
                return Decorate(sp, sp.GetRequiredService<MusicBrainzMusicProvider>(), new RateLimitOptions(options.PermitLimit, options.Window));
            },
        };

    public static IServiceCollection AddReveilMusical(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddWakeUpCore()
            .AddUserProfiles()
            .AddMusicProviders(configuration)
            .AddNotificationChannels(configuration);

    /// <summary>Services métier.</summary>
    public static IServiceCollection AddWakeUpCore(this IServiceCollection services)
    {
        services.AddSingleton<IWakeUpService, WakeUpService>();
        services.AddSingleton<ITrackSelector, WeatherTrackSelector>();
        services.AddSingleton<IMessageComposer, FrenchMessageComposer>();
        services.AddSingleton<IMusicResolver, MusicResolver>();
        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();
        services.AddLogging();
        return services;
    }

    public static IServiceCollection AddUserProfiles(this IServiceCollection services)
    {
        services.AddSingleton<IUserProfileProvider, InMemoryUserProfileProvider>();
        return services;
    }

    public static IServiceCollection AddMusicProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        services.Configure<MusicOptions>(configuration.GetSection(MusicOptions.SectionName));
        services.Configure<ItunesOptions>(configuration.GetSection(ItunesOptions.SectionName));
        services.AddOptions<MusicBrainzOptions>()
            .Bind(configuration.GetSection(MusicBrainzOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Contact), "MusicBrainz exige un contact dans le User-Agent (Music:MusicBrainz:Contact).");

        services.AddHttpClient(ItunesOptions.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ItunesOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = options.Timeout;
        });
        services.AddHttpClient(MusicBrainzOptions.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MusicBrainzOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        });

        services.AddSingleton<ItunesMusicProvider>();
        services.AddSingleton<MusicBrainzMusicProvider>();

        // L'ordre d'enregistrement donne l'ordre de bascule dans MusicResolver.
        var order = configuration.GetSection(MusicOptions.SectionName).Get<MusicOptions>()?.Providers ?? [];
        foreach (var name in order)
        {
            if (!MusicProviderFactories.TryGetValue(name, out var factory))
            {
                throw new InvalidOperationException(
                    $"Fournisseur musical inconnu '{name}'. Valeurs possibles : {string.Join(", ", MusicProviderFactories.Keys)}.");
            }

            services.AddSingleton(factory);
        }

        services.AddSingleton<IFallbackMusicProvider, LocalFallbackMusicProvider>();
        return services;
    }

    public static IServiceCollection AddNotificationChannels(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MockOutageOptions>(configuration.GetSection(MockOutageOptions.SectionName));

        // « SDK » simulés.
        services.AddSingleton<IMailSender, FakeMailSender>();
        services.AddSingleton<ISmsGateway, FakeSmsGateway>();
        services.AddSingleton<IPushService, FakePushService>();

        // Adaptateurs vers l'interface commune. Nouveau canal (WhatsApp…) = un adaptateur + une ligne ici.
        services.AddSingleton<INotificationChannel, EmailChannelAdapter>();
        services.AddSingleton<INotificationChannel, SmsChannelAdapter>();
        services.AddSingleton<INotificationChannel, PushChannelAdapter>();
        return services;
    }

    /// <summary>Empile quota puis cache autour d'un fournisseur : cache → quota → API.</summary>
    private static IMusicProvider Decorate(IServiceProvider sp, IMusicProvider provider, RateLimitOptions limits)
    {
        var rateLimited = ActivatorUtilities.CreateInstance<RateLimitedMusicProvider>(sp, provider, limits);
        var duration = sp.GetRequiredService<IOptions<MusicOptions>>().Value.CacheDuration;
        return ActivatorUtilities.CreateInstance<CachingMusicProvider>(sp, rateLimited, sp.GetRequiredService<IMemoryCache>(), duration);
    }
}
