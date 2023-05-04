using Microsoft.Extensions.Logging;

namespace Akka.Hosting.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services
                .AddAkka("TestSys", config =>
                {
                    config.WithActors((system, registry) =>
                    {
                        var echo = system.ActorOf(ClickActor.Props);
                        registry.Register<ClickActor>(echo);
                    });
                })
                .AddTransient<IMauiInitializeService, AkkaInitializer>()
                .AddTransient<MainPage>();

#if DEBUG
		    builder.Logging.AddDebug();
#endif
            var app = builder.Build();
            return app;
        }
    }
    
    public class AkkaInitializer: IMauiInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            AkkaMauiSupport.StartAkka(services).GetAwaiter().GetResult();
        }
    }
}