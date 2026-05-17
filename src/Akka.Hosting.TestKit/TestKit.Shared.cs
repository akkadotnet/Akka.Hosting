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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Sdk;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.TestKit
{
    public abstract partial class TestKit : TestKitBase
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
                if (_host is null)
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
            if (this is not INoImplicitSender && InternalCurrentActorCellKeeper.Current == null && TestActor != null)
                InternalCurrentActorCellKeeper.Current = (ActorCell)((ActorRefWithCell)TestActor).Underlying;
        }

        public TimeSpan StartupTimeout { get; }
        public string ActorSystemName { get; }
        public XunitTestOutputHelper? Output { get; }
        public LogLevel LogLevel { get; }

        private readonly TaskCompletionSource<Done> _initialized = new TaskCompletionSource<Done>();

        protected TestKit(string? actorSystemName = null, XunitTestOutputHelper? output = null, TimeSpan? startupTimeout = null,
            LogLevel logLevel = LogLevel.Information)
            : base(Assertions)
        {
            ActorSystemName = actorSystemName ?? "test";
            Output = output;
            LogLevel = logLevel;
            StartupTimeout = startupTimeout ?? TimeSpan.FromSeconds(30);
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

                // Don't re-register TestEventListener here — DefaultConfig (prepended above) already
                // configures it with a short type name. AddLogger<T> produces a fully-qualified name,
                // which makes InjectTopLevelFallback think the config changed and triggers a full
                // serialization rebuild. Under CI load that rebuild can race with scheduler disposal.
                builder.ConfigureLoggers(logger =>
                {
                    logger.LogLevel = ToAkkaLogLevel(LogLevel);
                });

                if (Output is { })
                {
                    builder.StartActors(async (system, registry) => { await LoggerHook(system, registry); });
                }

                // Register TestProbe using StartActors (not AddStartup) so it runs BEFORE user's WithActors
                // This ensures TestProbe is available for any actors that depend on IRequiredActor<TestProbe>
                builder.StartActors((actorSystem, actorRegistry) =>
                {
                    // Initialize TestActor here to ensure it's available before user actors start
                    base.InitializeTest(actorSystem, (ActorSystemSetup)null!, null, null);
                    actorRegistry.Register<TestProbe>(TestActor);

                    // Set implicit sender on initialization thread
                    if (this is not INoImplicitSender)
                        InternalCurrentActorCellKeeper.Current = (ActorCell)((ActorRefWithCell)TestActor).Underlying;
                });

                // User configuration comes AFTER TestProbe registration
                // Their WithActors/StartActors will be added after ours
                ConfigureAkka(builder, provider);

                builder.AddStartup((_, _) => { _initialized.TrySetResult(Done.Instance); });
            });
        }

        internal virtual Task LoggerHook(ActorSystem system, IActorRegistry registry)
        {
            var extSystem = (ExtendedActorSystem)system;
            var loggerName = $"log-test-{Guid.NewGuid():N}";
            var logger = extSystem.SystemActorOf(Props.Create(() => new TestKitLoggerFactoryLogger()), loggerName);
            // Fire and forget the logger initialization to avoid blocking
            // The logger will eventually initialize itself
            logger.Tell(new InitializeLogger(system.EventStream), ActorRefs.NoSender);
            return Task.CompletedTask;
        }

        protected virtual Config? Config { get; } = null;

        protected virtual void ConfigureLogging(ILoggingBuilder builder)
        { }

        protected abstract void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider);

        [InternalApi]
        private async Task InitializeAsyncCore()
        {
            var hostBuilder = new HostBuilder();
            if (Output != null)
            {
                hostBuilder.ConfigureLogging(logger =>
                {
                    logger.ClearProviders();
                    logger.AddProvider(new XUnitLoggerProvider(Output, LogLevel));
                    logger.AddFilter("Akka.*", LogLevel);
                    ConfigureLogging(logger);
                });
            }

            hostBuilder
                .ConfigureHostConfiguration(ConfigureHostConfiguration)
                .ConfigureAppConfiguration(ConfigureAppConfiguration);
            ConfigureHostBuilder(hostBuilder);
            hostBuilder.ConfigureServices(InternalConfigureServices);

            _host = hostBuilder.Build();

            using var cts = new CancellationTokenSource(StartupTimeout);
            try
            {
                await _host.StartAsync(cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException($"Host failed to start within {StartupTimeout.TotalSeconds} seconds");
            }

            // Wait for Akka initialization with timeout
            var initializedTask = _initialized.Task;
            var timeoutTask = Task.Delay(StartupTimeout, CancellationToken.None);
            if (await Task.WhenAny(initializedTask, timeoutTask) == timeoutTask)
                throw new TimeoutException($"Akka.NET failed to initialize within {StartupTimeout.TotalSeconds} seconds");

            // TestActor initialization and registration now happens in AddStartup
            // before user actors are created, preventing race conditions

            await BeforeTestStart();
        }

        protected sealed override void InitializeTest(ActorSystem system, ActorSystemSetup config, string actorSystemName,
            string testActorName)
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

        private async Task DisposeAsyncCore()
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
