using System;
using System.Collections.Generic;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Journal;
using Akka.Util;

namespace Akka.Persistence.Hosting
{
    public enum PersistenceMode
    {
        /// <summary>
        /// Sets both the akka.persistence.journal and the akka.persistence.snapshot-store to use this plugin.
        /// </summary>
        Both,

        /// <summary>
        /// Sets ONLY the akka.persistence.journal to use this plugin.
        /// </summary>
        Journal,

        /// <summary>
        /// Sets ONLY the akka.persistence.snapshot-store to use this plugin.
        /// </summary>
        SnapshotStore,
    }
    
    /// <summary>
    /// Used to help build journal configurations
    /// </summary>
    public sealed class AkkaPersistenceJournalBuilder
    {
        internal readonly string JournalId;
        internal readonly AkkaConfigurationBuilder Builder;
        internal readonly Dictionary<Type, HashSet<string>> Bindings = new Dictionary<Type, HashSet<string>>();
        internal readonly Dictionary<string, Type> Adapters = new Dictionary<string, Type>();

        public AkkaPersistenceJournalBuilder(string journalId, AkkaConfigurationBuilder builder)
        {
            JournalId = journalId;
            Builder = builder;
        }

        public AkkaPersistenceJournalBuilder AddEventAdapter<TAdapter>(string eventAdapterName,
            IEnumerable<Type> boundTypes) where TAdapter : IEventAdapter
        {
            AddAdapter<TAdapter>(eventAdapterName, boundTypes);

            return this;
        }

        public AkkaPersistenceJournalBuilder AddReadEventAdapter<TAdapter>(string eventAdapterName,
            IEnumerable<Type> boundTypes) where TAdapter : IReadEventAdapter
        {
            AddAdapter<TAdapter>(eventAdapterName, boundTypes);

            return this;
        }
        
        public AkkaPersistenceJournalBuilder AddWriteEventAdapter<TAdapter>(string eventAdapterName,
            IEnumerable<Type> boundTypes) where TAdapter : IWriteEventAdapter
        {
            AddAdapter<TAdapter>(eventAdapterName, boundTypes);

            return this;
        }
        
        private void AddAdapter<TAdapter>(string eventAdapterName, IEnumerable<Type> boundTypes)
        {
            Adapters[eventAdapterName] = typeof(TAdapter);
            foreach (var t in boundTypes)
            {
                if (!Bindings.ContainsKey(t))
                    Bindings[t] = new HashSet<string>();
                Bindings[t].Add(eventAdapterName);
            }
        }

        /// <summary>
        /// INTERNAL API - Builds the HOCON and then injects it.
        /// </summary>
        internal void Build()
        {
            // useless configuration - don't bother.
            if (Adapters.Count == 0 || Bindings.Count == 0)
                return;

            var adapters = new StringBuilder()
                .Append($"akka.persistence.journal.{JournalId}").Append("{")
                .AppendLine("event-adapters {");
            foreach (var kv in Adapters)
            {
                adapters.AppendLine($"{kv.Key} = \"{kv.Value.TypeQualifiedName()}\"");
            }

            adapters.AppendLine("}").AppendLine("event-adapter-bindings {");
            foreach (var kv in Bindings)
            {
                adapters.AppendLine($"\"{kv.Key.TypeQualifiedName()}\" = [{string.Join(",", kv.Value)}]");
            }

            adapters.AppendLine("}").AppendLine("}");

            var finalHocon = ConfigurationFactory.ParseString(adapters.ToString());
            Builder.AddHocon(finalHocon, HoconAddMode.Prepend);
        }
    }

    /// <summary>
    /// The set of options for generic Akka.Persistence.
    /// </summary>
    public static class AkkaPersistenceHostingExtensions
    {
        /// <summary>
        /// Used to configure a specific Akka.Persistence.Journal instance, primarily to support <see cref="IEventAdapter"/>s.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="journalId">The id of the journal. i.e. if you want to apply this adapter to the `akka.persistence.journal.sql` journal, just type `sql`.</param>
        /// <param name="journalBuilder">Configuration method for configuring the journal.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <remarks>
        /// This method can be called multiple times for different <see cref="IEventAdapter"/>s.
        /// </remarks>
        public static AkkaConfigurationBuilder WithJournal(this AkkaConfigurationBuilder builder,
            string journalId, Action<AkkaPersistenceJournalBuilder> journalBuilder)
        {
            var jBuilder = new AkkaPersistenceJournalBuilder(journalId, builder);
            journalBuilder(jBuilder);

            // build and inject the HOCON
            jBuilder.Build();
            return builder;
        }
    }
}