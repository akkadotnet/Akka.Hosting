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
using Akka.Annotations;
using Akka.Configuration;
using Akka.Hosting.TestKit.Internals;
using Akka.TestKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Akka.Hosting.TestKit
{
    public abstract partial class HostingSpec: IAsyncLifetime
    {
        private IHost _host;
        public IHost Host
        {
            get
            {
                AssertNotNull(_host);
                return _host;
            }
        }

        private TestKitBaseUnWrapper _testKit;
        public Akka.TestKit.Xunit2.TestKit TestKit
        {
            get
            {
                AssertNotNull(_testKit);
                return _testKit;
            }
        }

        public ActorRegistry ActorRegistry => Host.Services.GetRequiredService<ActorRegistry>();
        
        public TimeSpan StartupTimeout { get; }
        public string ActorSystemName { get; }
        public ITestOutputHelper Output { get; }
        public LogLevel LogLevel { get; }

        protected HostingSpec(string actorSystemName, ITestOutputHelper output = null, TimeSpan? startupTimeout = null, LogLevel logLevel = LogLevel.Information)
        {
            ActorSystemName = actorSystemName;
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
        
        private void InternalConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            ConfigureServices(context, services);
            
            services.AddAkka(ActorSystemName, (builder, provider) =>
            {
                builder.AddHocon(
                    Config != null ? Config.WithFallback(TestKitBase.DefaultConfig) : TestKitBase.DefaultConfig,
                    HoconAddMode.Prepend);

                ConfigureAkka(builder, provider);
            });
        }

        protected virtual Config Config { get; } = null;
        
        protected virtual void ConfigureLogging(ILoggingBuilder builder)
        { }

        protected virtual void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        { }
        
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
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureServices(InternalConfigureServices);

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

            _sys = _host.Services.GetRequiredService<ActorSystem>();
            _testKit = new TestKitBaseUnWrapper(_sys, Output);

            if (this is INoImplicitSender)
            {
                InternalCurrentActorCellKeeper.Current = null;
            }
            else
            {
                InternalCurrentActorCellKeeper.Current = (ActorCell)((ActorRefWithCell)_testKit.TestActor).Underlying;
            }
            SynchronizationContext.SetSynchronizationContext(
                new ActorCellKeepingSynchronizationContext(InternalCurrentActorCellKeeper.Current));

            await BeforeTestStart();
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
        protected virtual async Task AfterAllAsync()
        {
            await ShutdownAsync();
        }

        public async Task DisposeAsync()
        {
            await AfterAllAsync();
            if(_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
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
        
        private static void AssertNotNull(object obj)
        {
            if(obj is null)
                throw new XunitException("Test has not been initialized yet"); 
        }
    }    
}

