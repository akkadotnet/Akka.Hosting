namespace Akka.Hosting.Maui;

/// <summary>
/// INTERNAL API
/// </summary>
/// <remarks>
/// Runs before any UI elements in MAUI do.
/// </remarks>
internal sealed class MauiAkkaService : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var akkaService = services.GetRequiredService<AkkaHostedService>();
        akkaService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}