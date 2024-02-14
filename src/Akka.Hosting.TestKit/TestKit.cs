﻿// -----------------------------------------------------------------------
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
using Akka.Hosting.TestKit.Internals;
using Akka.TestKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.TestKit
{
    /// <summary>
    /// <remarks>Unless you're creating a Hosting TestKit for a specific test framework, you should probably not inherit directly from this class.</remarks>
    /// </summary>
    public abstract class TestKitBase: Akka.TestKit.TestKitBase
    {
        private IHost? _host;
        public IHost Host
        {
            get
            {
                if(_host is null)
                    ThrowNotInitialized();
                return _host;
            }
        }
        
        protected abstract void ThrowNotInitialized();

        public ActorRegistry ActorRegistry => Host.Services.GetRequiredService<ActorRegistry>();
        
        public TimeSpan StartupTimeout { get; }
        public string ActorSystemName { get; }
        public LogLevel LogLevel { get; }

        private readonly TaskCompletionSource<Done> _initialized = new TaskCompletionSource<Done>();

        protected TestKitBase(ITestKitAssertions assertions, string? actorSystemName = null, TimeSpan? startupTimeout = null, LogLevel logLevel = LogLevel.Information)
        : base(assertions)
        {
            ActorSystemName = actorSystemName ?? "test";
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

                if (ShouldUseCustomLogger)
                {
                    builder.StartActors(async (system, registry) =>
                    {
                        await LoggerHook(system, registry);
                    });
                }

                ConfigureAkka(builder, provider);

                builder.AddStartup((system, _) =>
                {
                    try
                    {
                        base.InitializeTest(system, (ActorSystemSetup)null!, null, null);
                        _initialized.SetResult(Done.Instance);
                    }
                    catch (Exception e)
                    {
                        _initialized.SetException(e);
                    }
                });
            });
        }

        protected virtual bool ShouldUseCustomLogger => false; 
        
        protected virtual async Task LoggerHook(ActorSystem system, IActorRegistry registry)
        {
            var extSystem = (ExtendedActorSystem)system;
            var logger = extSystem.SystemActorOf(Props.Create(() => new TestKitLoggerFactoryLogger()), "log-test");
            await logger.Ask<LoggerInitialized>(new InitializeLogger(system.EventStream));
        }

        protected virtual Config? Config { get; } = null;
        
        protected virtual void ConfigureLogging(ILoggingBuilder builder)
        { }

        protected abstract void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider);
        
        [InternalApi]
        public async Task InitializeAsync()
        {
            var hostBuilder = new HostBuilder();
            if (ShouldUseCustomLogger)
                hostBuilder.ConfigureLogging(ConfigureLogging);
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
            
            if (this is not INoImplicitSender && InternalCurrentActorCellKeeper.Current is null)
                InternalCurrentActorCellKeeper.Current = (ActorCell)((ActorRefWithCell)TestActor).Underlying;
            
            await BeforeTestStart();
        }

        protected sealed override void InitializeTest(ActorSystem system, ActorSystemSetup config, string actorSystemName, string testActorName)
        {
            // no-op, deferring InitializeTest after Host have ran
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

