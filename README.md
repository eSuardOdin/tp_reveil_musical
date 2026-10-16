# Réveil musical

Service qui réveille un utilisateur avec un morceau choisi selon **le jour de la semaine** et **la météo du jour**,
puis le prévient sur **son canal préféré** (email, SMS, push). Implémenté en **C# / .NET 10 (LTS)**.

```bash
dotnet run --project src/ReveilMusical.App -- alice lundi PLUIE
```

```
info: …FakeMailSender[0] [MOCK EMAIL] À: alice@example.com | Sujet: Bon lundi ! | <p>Il pleut aujourd'hui…
info: …WakeUpService[0] Réveil de alice : Riders on the Storm — The Doors (source iTunes) via email.

Morceau : Riders on the Storm — The Doors (source : iTunes)
Canal   : email
```

Arguments : `<userId> <jour> <météo>`. Jour en français (`lundi`), en anglais (`Monday`) ou en nombre (`1` = lundi, `0` = dimanche).
Météo : `SOLEIL` / `PLUIE` / `NEIGE` / `NUAGEUX`. Utilisateurs mockés : `alice` (email), `bob` (SMS, pas de morceau pour NEIGE),
`chloe` (push, ne couvre que NEIGE).

Code de sortie : `0` réveil délivré, `1` aucun canal n'a abouti, `2` entrée invalide ou utilisateur inconnu.

---

## 1. Des besoins métier aux choix techniques

| Exigence métier | Traduction technique |
|---|---|
| **Changer de fournisseur musical rapidement** | Port `IMusicProvider` dans le cœur. iTunes et MusicBrainz sont deux adaptateurs. L'ordre de bascule se règle dans `appsettings.json` (`Music:Providers`), **sans recompiler**. Un nouveau fournisseur = une classe + une entrée dans `MusicProviderFactories`. |
| **Canal au choix de l'utilisateur, nouveaux canaux à venir** | Port `INotificationChannel` identifié par une **clé texte** (`"email"`, `"sms"`…) et non par un `enum` : le métier ne connaît pas la liste des canaux. Ajouter WhatsApp = un adaptateur + une ligne `AddSingleton<INotificationChannel, WhatsAppChannelAdapter>()`, sans toucher au cœur. |
| **Mocks de notification aux interfaces différentes** | Trois « SDK » simulés volontairement incompatibles (`IMailSender` synchrone + HTML, `ISmsGateway` asynchrone avec codes de statut et 160 caractères, `IPushService` avec accusé de réception), ramenés à `INotificationChannel` par trois **adaptateurs** (pattern Adapter). |
| **Aucun composant externe sans vérification** | Inventaire complet en section 4 (licence, version installée, dernière stable, date). `dotnet list package --outdated` et `--vulnerable` sont propres. |
| **Jamais de silence** | Côté musique : `MusicResolver` essaie chaque fournisseur dans l'ordre (panne, quota ou résultat vide → suivant), puis `LocalFallbackMusicProvider` (liste codée en dur, sans réseau) qui **retourne toujours un morceau**. Côté notification : `NotificationDispatcher` essaie le canal préféré puis tous les autres canaux pour lesquels l'utilisateur a une coordonnée. Le résultat indique `IsDegraded`. |
| **Heure précise** | Pas d'attente ni de nouvelle tentative bloquante : timeout HTTP court (3 s, configurable), et quota atteint ⇒ bascule immédiate plutôt qu'attente. |
| **Quota iTunes ~20 req/min** | Décorateurs empilés autour de chaque fournisseur distant : `CachingMusicProvider` → `RateLimitedMusicProvider` → API. Les lectures en cache ne consomment pas de quota ; un même morceau demandé par 1 000 utilisateurs = 1 appel. MusicBrainz est limité à 1 req/s. |
| **User-Agent MusicBrainz** | Posé sur le client HTTP nommé à partir de `MusicBrainzOptions` (`App/Version ( contact )`). L'application **refuse de démarrer** sans contact configuré (validation d'options). |
| **`trackViewUrl` ne doit pas fuiter** | Les DTO iTunes/MusicBrainz sont `internal` à leur adaptateur. Le métier ne voit que `Track(Title, Artist, ListenUrl)`. Un test d'architecture le vérifie. |

### Isolation et faible couplage

```
┌──────────────── ReveilMusical.Core (aucune dépendance technique) ─────────────────┐
│  Domain : Track, UserProfile, WakeUpRequest, Weather, WakeUpResult…               │
│  Ports  : IMusicProvider, IFallbackMusicProvider, INotificationChannel,           │
│           IUserProfileProvider, ITrackSelector, IMessageComposer…                 │
│  Services : WakeUpService → WeatherTrackSelector → MusicResolver                  │
│                           → FrenchMessageComposer → NotificationDispatcher        │
└───────────────────────────────▲───────────────────────────────────────────────────┘
                                │ implémente les ports
┌───────────────────── ReveilMusical.Infrastructure ────────────────────────────────┐
│  Music/Itunes, Music/MusicBrainz (HTTP + DTO internes)                            │
│  Music/Decorators (cache, quota)        Music/Local (secours codé en dur)          │
│  Notifications/Vendors (SDK simulés)    Notifications/Adapters (→ port commun)     │
│  Profiles (mock du service interne)     DependencyInjection (racine de composition)│
└───────────────────────────────▲───────────────────────────────────────────────────┘
                                │
                    ReveilMusical.App (console : parse les arguments, appelle IWakeUpService)
```

- `ReveilMusical.Core` ne référence **ni** l'infrastructure, **ni** `System.Net.Http`, **ni** `System.Text.Json` : seulement
  `Microsoft.Extensions.Logging.Abstractions`. C'est vérifié par `ArchitectureTests`.
- Le flux d'un réveil : profil → choix du morceau (météo, sinon morceau de secours de l'utilisateur) → recherche
  (fournisseurs puis secours local, qui varie selon la météo et le jour) → message (« Bon lundi ! Il pleut… ») → envoi.

### IoC / DI

- Toutes les dépendances sont injectées par constructeur, en interfaces. Le conteneur `Microsoft.Extensions.DependencyInjection`
  instancie tous les services ; la seule configuration de câblage est `ServiceCollectionExtensions` (racine de composition).
- Les décorateurs sont construits via `ActivatorUtilities.CreateInstance`, pas par `new`.
- `CoreServices_DependOnlyOnAbstractions` vérifie par réflexion qu'aucun service métier ne dépend d'une classe concrète.
- Les `new` restants portent uniquement sur des **valeurs** (records `Track`, `WakeUpMessage`, DTO de payload,
  exceptions, collections) et non sur des implémentations de service. Les tests instancient directement les classes testées, ce qui est normal.

### Limite assumée

Le service de profils est interne : s'il est en panne, l'exception remonte (on ne sait pas qui contacter ni comment).
L'ordonnanceur, hors périmètre, est responsable de la nouvelle tentative. Si **tous** les canaux échouent, l'échec est
journalisé en `Critical` et `WakeUpResult.Delivered == false` (code de sortie 1), pour qu'il soit visible et que l'alerting le remonte.

---

## 2. Configuration et démonstration des modes dégradés

Tout est dans [`src/ReveilMusical.App/appsettings.json`](src/ReveilMusical.App/appsettings.json) et peut être surchargé par variables d'environnement.

```bash
# Changer de fournisseur : MusicBrainz d'abord, puis iTunes
Music__Providers__0=musicbrainz Music__Providers__1=itunes dotnet run --project src/ReveilMusical.App -- bob samedi NEIGE
```

```bash
# Panne simulée du SMS : bob est réveillé par email (mode dégradé)
Notifications__Mocks__Outages__0=sms dotnet run --project src/ReveilMusical.App -- bob lundi SOLEIL
```

```bash
# Fournisseurs injoignables : morceau du secours local, le réveil part quand même
Music__Itunes__BaseUrl=http://127.0.0.1:9/ Music__MusicBrainz__BaseUrl=http://127.0.0.1:9/ dotnet run --project src/ReveilMusical.App -- alice dimanche NUAGEUX
```

Contact MusicBrainz : remplacez `Music:MusicBrainz:Contact` par une adresse réelle de l'équipe avant la mise en production.

---

## 3. Tests

```bash
dotnet test
```

```bash
dotnet test --coverlet --coverlet-output-format cobertura
```

- **132 tests**, xUnit v3 sur Microsoft.Testing.Platform (`global.json`), doublures avec NSubstitute.
- Aucun appel réseau : les adaptateurs HTTP sont testés avec un `HttpMessageHandler` factice ; le quota avec `FakeTimeProvider`.
- Couverture (coverlet) : **100 % des lignes (444/444) et des branches (111/111)** de `Core`, `Infrastructure` et du parseur
  d'arguments. `Program.cs` (point d'entrée, 36 lignes) n'est pas instrumenté et a été vérifié à la main avec les scénarios ci-dessus.
- Organisation :
  - `Core/` : règles métier, bascule musique et canaux, orchestration.
  - `Infrastructure/` : adaptateurs iTunes et MusicBrainz (mapping, erreurs HTTP), cache et quota, adaptateurs de notification,
    mocks, câblage DI, et deux tests **de bout en bout** (iTunes KO → MusicBrainz, SMS KO → email ; tout KO → secours local).
  - `ArchitectureTests` : isolation du cœur, dépendances abstraites, DTO non exposés.

---

## 4. Inventaire des dépendances

Vérifié le **12/10/2026** avec `dotnet list package --outdated` / `--vulnerable --include-transitive`
et l'API NuGet. **Toutes les dépendances directes sont à la dernière version stable, aucune vulnérabilité connue,
aucune licence copyleft.**

### SDK / runtime

| Composant | Licence | Version installée | Dernière stable | Commentaire |
|---|---|---|---|---|
| .NET SDK / runtime | MIT | SDK 10.0.401 / runtime 10.0.12 | 10.0.x (LTS, supportée jusqu'à nov. 2028) | `global.json` impose ≥ 10.0.100 |

### Dépendances livrées avec l'application

| Paquet | Projet | Licence | Installée | Dernière stable | Publiée le | Usage |
|---|---|---|---|---|---|---|
| Microsoft.Extensions.Logging.Abstractions | Core, Infrastructure | MIT | 10.0.12 | 10.0.12 | 08/09/2026 | `ILogger<T>` (seule dépendance du cœur) |
| Microsoft.Extensions.Http | Infrastructure | MIT | 10.0.12 | 10.0.12 | 08/09/2026 | `IHttpClientFactory`, clients nommés |
| Microsoft.Extensions.Caching.Memory | Infrastructure | MIT | 10.0.12 | 10.0.12 | 08/09/2026 | Cache des recherches (quota iTunes) |
| Microsoft.Extensions.Options.ConfigurationExtensions | Infrastructure | MIT | 10.0.12 | 10.0.12 | 08/09/2026 | Options typées et validées |
| Microsoft.Extensions.Hosting | App | MIT | 10.0.12 | 10.0.12 | 08/09/2026 | Conteneur DI, configuration, logs console |

Les 31 paquets transitifs de l'application sont tous des `Microsoft.Extensions.*` / `System.*` en **10.0.12, licence MIT**.

### Dépendances de test (non livrées)

| Paquet | Licence | Installée | Dernière stable | Publiée le | Usage |
|---|---|---|---|---|---|
| xunit.v3 | Apache-2.0 | 4.0.2 | 4.0.2 | 09/10/2026 | Framework de test |
| NSubstitute | BSD-3-Clause | 6.2.0 | 6.2.0 | 11/08/2026 | Doublures de test |
| Castle.Core | Apache-2.0 | 5.2.1 | 5.2.1 | 09/03/2025 | Épinglé explicitement (voir plus bas) |
| coverlet.MTP | MIT | 10.1.0 | 10.1.0 | 27/09/2026 | Couverture de code |
| Microsoft.Extensions.TimeProvider.Testing | MIT | 10.10.0 | 10.10.0 | 09/09/2026 | `FakeTimeProvider` (tests du quota) |

### Points d'attention et justifications

- **NSubstitute plutôt que Moq** : Moq a embarqué en 2023 (v4.20) un composant (*SponsorLink*) qui lisait l'email Git
  des développeurs à la compilation. Même retiré depuis, c'est exactement le genre de risque que la direction veut éviter.
  NSubstitute (BSD-3) n'a pas cet historique.
- **Castle.Core** (dépendance de NSubstitute) se résolvait en 5.1.1 (déc. 2022). Il est **épinglé en 5.2.1** (dernière stable) dans le projet de test.
- **Transitifs anciens côté test uniquement** : `Microsoft.Bcl.AsyncInterfaces 6.0.0` et `Microsoft.Win32.Registry 5.0.0`
  (via `xunit.v3.runner.common`), `System.Security.AccessControl 6.0.1`. Tous MIT, sans vulnérabilité connue. `Win32.Registry` et
  `AccessControl` sont à leur dernière version (paquets gelés, intégrés au runtime). Ils ne sont pas livrés avec l'application.
- **Microsoft.ApplicationInsights 2.23.0** (MIT, transitif via `Microsoft.Testing.Extensions.Telemetry`, tiré par xunit.v3) :
  télémétrie du **runner de test**, pas de l'application. Une version 3.x existe, mais la version est fixée par Microsoft.Testing.Platform.
  Pour la désactiver : `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` (et `DOTNET_CLI_TELEMETRY_OPTOUT=1` pour le SDK).
- **Pas de Polly / Microsoft.Extensions.Http.Resilience** : la bascule entre fournisseurs, le timeout et le quota sont gérés
  par environ 60 lignes de code testées à 100 %. Une dépendance de plus ne se justifiait pas ici.

### APIs externes

| API | Conditions | Respect dans le code |
|---|---|---|
| iTunes Search API | Gratuite, sans clé, ~20 req/min | Cache 12 h + quota 20/min en fenêtre glissante |
| MusicBrainz WS/2 | Gratuite, User-Agent identifiable obligatoire, 1 req/s. Données sous CC0 / CC BY-NC-SA | User-Agent validé au démarrage, quota 1 req/s. On n'utilise que titre et artiste (CC0) |

---

## 5. Structure du dépôt

```
ReveilMusical.slnx
global.json                         SDK .NET 10 + runner Microsoft.Testing.Platform
Directory.Build.props               nullable, warnings traités comme des erreurs
src/
  ReveilMusical.Core/               Domain/, Ports/, Services/
  ReveilMusical.Infrastructure/     Music/, Notifications/, Profiles/, DependencyInjection/
  ReveilMusical.App/                Program.cs, WakeUpRequestParser.cs, appsettings.json
tests/
  ReveilMusical.Tests/              Core/, Infrastructure/, App/, ArchitectureTests.cs
```
