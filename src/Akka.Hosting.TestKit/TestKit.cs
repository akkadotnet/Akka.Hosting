// -----------------------------------------------------------------------
//  <copyright file="HostingSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Actor.Setup;
using Akka.Annotations;
using Akka.Configuration;
using Akka.Event;
using Akka.Hosting.Logging;
using Akka.Hosting.TestKit.Internals;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Akka.TestKit.Xunit2.Internals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.TestKit
{
    public abstract class TestKit: TestKitBase, IAsyncLifetime
    {
        /// <summary>
        /// Commonly used assertions used throughout the testkit.
        /// </summary>
        protected static XunitAssertions Assertions { get; } = new XunitAssertions();

        private IHost? _host;
        public IHost Host
        {
            get
            {
                if(_host is null)
                    throw new XunitException("Test has not been initialized yet");
                    
                // Ensure implicit sender is set on current thread when accessing Host
                EnsureImplicitSender();
                return _host;
            }
        }

        public ActorRegistry ActorRegistry => Host.Services.GetRequiredService<ActorRegistry>();
        
        /// <summary>
        /// Ensures the implicit sender is set on the current thread.
        /// Called automatically when accessing Host or Sys.
        /// </summary>
        private void EnsureImplicitSender()
        {
            if (this is not INoImplicitSender && TestActor != null)
            {
                InternalCurrentActorCellKeeper.Current = (ActorCell)((ActorRefWithCell)TestActor).Underlying;
            }
        }
        
        public TimeSpan StartupTimeout { get; }
        public string ActorSystemName { get; }
        public ITestOutputHelper? Output { get; }
        public LogLevel LogLevel { get; }

        private readonly TaskCompletionSource<Done> _initialized = new TaskCompletionSource<Done>();

        protected TestKit(string? actorSystemName = null, ITestOutputHelper? output = null, TimeSpan? startupTimeout = null, LogLevel logLevel = LogLevel.Information)
        : base(Assertions)
        {
            ActorSystemName = actorSystemName ?? "test";
            Output = output;
            LogLevel = logLevel;
            StartupTimeout = startupTimeout ?? TimeSpan.FromSeconds(10);
        }
        
        protected virtual void ConfigureHostConfiguration(IConfigurationBuilder builder)
        { }
        
        protected virtual void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
        { }

        protected virtual void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        { }
        
        protected virtual void ConfigureHostBuilder(IHostBuilder builder)
        { }
        
        private void InternalConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            ConfigureServices(context, services);
            
            services.AddAkka(ActorSystemName, (builder, provider) =>
            {
                builder.AddHocon(DefaultConfig, HoconAddMode.Prepend);
                if (Config is { })
                    builder.AddHocon(Config, HoconAddMode.Prepend);

                builder.ConfigureLoggers(logger =>
                {
                    logger.LogLevel = ToAkkaLogLevel(LogLevel);
                    logger.ClearLoggers();
                    logger.AddLogger<TestEventListener>();
                });

                if (Output is { })
                {
                    builder.StartActors(async (system, registry) =>
                    {
                        await LoggerHook(system, registry);
                    });
                }

                ConfigureAkka(builder, provider);

                builder.AddStartup((_, _) =>
                {
                    _initialized.TrySetResult(Done.Instance);
                });
            });
        }

        internal virtual Task LoggerHook(ActorSystem system, IActorRegistry registry)
        {
            var extSystem = (ExtendedActorSystem)system;
            var loggerName = $"log-test-{Guid.NewGuid():N}";
            var logger = extSystem.SystemActorOf(Props.Create(() => new TestKitLoggerFactoryLogger()), loggerName);
            // Fire and forget the logger initialization to avoid blocking
            // The logger will eventually initialize itself
            logger.Tell(new InitializeLogger(system.EventStream));
            return Task.CompletedTask;
        }

        protected virtual Config? Config { get; } = null;
        
        protected virtual void ConfigureLogging(ILoggingBuilder builder)
        { }

        protected abstract void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider);
        
        [InternalApi]
        public async Task InitializeAsync()
        {
            var hostBuilder = new HostBuilder();
            if (Output != null)
                hostBuilder.ConfigureLogging(logger =>
                {
                    logger.ClearProviders();
                    logger.AddProvider(new XUnitLoggerProvider(Output, LogLevel));
                    logger.AddFilter("Akka.*", LogLevel);
                    ConfigureLogging(logger);
                });
            hostBuilder
                .ConfigureHostConfiguration(ConfigureHostConfiguration)
                .ConfigureAppConfiguration(ConfigureAppConfiguration);
            ConfigureHostBuilder(hostBuilder);
            hostBuilder.ConfigureServices(InternalConfigureServices);

            _host = hostBuilder.Build();

            var cts = new CancellationTokenSource(StartupTimeout);
            cts.Token.Register(() =>
                throw new TimeoutException($"Host failed to start within {StartupTimeout.Seconds} seconds"));
            try
            {
                await _host.StartAsync(cts.Token);
            }
            finally
            {
                cts.Dispose();
            }

            await _initialized.Task;
            
            var system = _host.Services.GetRequiredService<ActorSystem>();
            var registry = _host.Services.GetRequiredService<ActorRegistry>();
            
            // Initialize TestActor directly without synchronization context posting
            // The implicit sender will be set on the current thread after initialization
            base.InitializeTest(system, (ActorSystemSetup)null!, null, null);
            
            registry.Register<TestProbe>(TestActor);
            
            // ALWAYS set the implicit sender context on the current thread after initialization
            // This ensures it's available on the thread where tests will run
            // This is critical for tests using DI-created actors
            if (this is not INoImplicitSender)
            {
                InternalCurrentActorCellKeeper.Current = (ActorCell)((ActorRefWithCell)TestActor).Underlying;
            }
            
            await BeforeTestStart();
        }

        protected sealed override void InitializeTest(ActorSystem system, ActorSystemSetup config, string actorSystemName, string testActorName)
        {
            // no-op, deferring InitializeTest after Host have ran
        }
        
        /// <summary>
        /// Override Sys property to ensure implicit sender is set when accessing the actor system
        /// </summary>
        public new ActorSystem Sys 
        { 
            get 
            {
                EnsureImplicitSender();
                return base.Sys;
            }
        }

        protected virtual Task BeforeTestStart()
        {
            // Ensure the implicit sender is set on the current thread before each test
            // This is critical because tests may run on different threads than initialization
            if (this is not INoImplicitSender)
            {
                InternalCurrentActorCellKeeper.Current = (ActorCell)((ActorRefWithCell)TestActor).Underlying;
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// This method is called when a test ends.
        ///
        /// <remarks>
        /// If you override this, then make sure you either call base.AfterAllAsync()
        /// to shut down the system. Otherwise a memory leak will occur.
        /// </remarks>
        /// </summary>
        protected virtual Task AfterAllAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            Exception? exception = null;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await Task.WhenAny(Task.Delay(Timeout.Infinite, cts.Token), AfterAllAsync());
                if (cts.IsCancellationRequested)
                    throw new TimeoutException($"{nameof(AfterAllAsync)} took more than 5 seconds to execute, aborting.");
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                try
                {
                    Shutdown();
                    if (_host != null)
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await _host.StopAsync(cts.Token);
                    }
                }
                catch
                {
                    // no-op
                }
                finally
                {
                    _host?.Dispose();
                }
                
                if (exception is { })
                    throw exception;
            }
        }

        private static Event.LogLevel ToAkkaLogLevel(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.Trace => Event.LogLevel.DebugLevel,
                LogLevel.Debug => Event.LogLevel.DebugLevel,
                LogLevel.Information => Event.LogLevel.InfoLevel,
                LogLevel.Warning => Event.LogLevel.WarningLevel,
                LogLevel.Error => Event.LogLevel.ErrorLevel,
                LogLevel.Critical => Event.LogLevel.ErrorLevel,
                _ => Event.LogLevel.ErrorLevel
            };
        
    }    
}

