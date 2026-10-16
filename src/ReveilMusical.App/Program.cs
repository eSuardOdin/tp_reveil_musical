using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReveilMusical.App;
using ReveilMusical.Core.Ports;
using ReveilMusical.Infrastructure.DependencyInjection;

// Usage : dotnet run --project src/ReveilMusical.App -- <userId> <jour> <météo>
// Exemple : dotnet run --project src/ReveilMusical.App -- alice lundi PLUIE
if (args.Length != 3)
{
    Console.Error.WriteLine("Usage : ReveilMusical.App <userId> <jour> <SOLEIL|PLUIE|NEIGE|NUAGEUX>");
    return 2;
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Services.AddReveilMusical(builder.Configuration);
using var host = builder.Build();

try
{
    var request = WakeUpRequestParser.Parse(args[0], args[1], args[2]);
    var result = await host.Services.GetRequiredService<IWakeUpService>().SendWakeUpAsync(request);

    Console.WriteLine();
    Console.WriteLine($"Morceau : {result.Track} (source : {result.MusicSource})");
    Console.WriteLine($"Canal   : {result.Channel ?? "AUCUN"}{(result.IsDegraded ? " — mode dégradé" : string.Empty)}");
    return result.Delivered ? 0 : 1;
}
catch (Exception ex) when (ex is ArgumentException or ReveilMusical.Core.Domain.UnknownUserException)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}
