using System.Reflection;
using ReveilMusical.Core.Ports;
using ReveilMusical.Core.Services;
using ReveilMusical.Infrastructure.DependencyInjection;

namespace ReveilMusical.Tests;

/// <summary>Garde-fous sur l'exigence d'isolation et d'IoC.</summary>
public class ArchitectureTests
{
    private static readonly Assembly Core = typeof(WakeUpService).Assembly;
    private static readonly Assembly Infrastructure = typeof(ServiceCollectionExtensions).Assembly;

    [Fact]
    public void Core_DoesNotReferenceInfrastructureOrHttp()
    {
        var references = Core.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.DoesNotContain("ReveilMusical.Infrastructure", references);
        Assert.DoesNotContain("System.Net.Http", references);
        Assert.DoesNotContain("System.Text.Json", references);
    }

    [Fact]
    public void CoreServices_DependOnlyOnAbstractions()
    {
        var concreteDependencies = Core.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Namespace == typeof(WakeUpService).Namespace)
            .SelectMany(t => t.GetConstructors().SelectMany(c => c.GetParameters()), (t, p) => (Type: t, Parameter: p.ParameterType))
            .Where(x => !x.Parameter.IsInterface && !IsEnumerableOfInterface(x.Parameter))
            .Select(x => $"{x.Type.Name}({x.Parameter.Name})")
            .ToList();

        Assert.Empty(concreteDependencies);
    }

    [Fact]
    public void ProviderDtos_DoNotLeakOutOfInfrastructure()
    {
        var publicDtos = Infrastructure.GetExportedTypes()
            .Where(t => t.Name.Contains("Response") || t.Name.StartsWith("Itunes") && t.Name != "ItunesMusicProvider" && t.Name != "ItunesOptions")
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(publicDtos);
    }

    [Fact]
    public void EveryChannelAdapter_ImplementsCommonInterface()
    {
        var adapters = Infrastructure.GetTypes().Where(t => t.Name.EndsWith("ChannelAdapter")).ToList();

        Assert.NotEmpty(adapters);
        Assert.All(adapters, a => Assert.True(typeof(INotificationChannel).IsAssignableFrom(a), a.Name));
    }

    private static bool IsEnumerableOfInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) && type.GetGenericArguments()[0].IsInterface;
}
