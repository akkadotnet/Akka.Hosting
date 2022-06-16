using System;
using System.Collections.Generic;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Journal;

namespace Akka.Persistence.Hosting
{
    /// <summary>
    /// The set of options for generic Akka.Persistence.
    /// </summary>
    public static class AkkaPersistenceHostingExtensions
    {
        /// <summary>
        /// Adds an <see cref="IEventAdapter"/> to a specific `akka.persistence.journal`.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="journalId">The id of the journal. i.e. if you want to apply this adapter to the `akka.persistence.journal.sql` journal, just type `sql`.</param>
        /// <param name="eventAdapterName">The short-hand name for this event-adapter. Needed for populating bindings.</param>
        /// <param name="boundTypes">The set of event types that should be handled by <see cref="TAdapter"/>.</param>
        /// <typeparam name="TAdapter">The type of <see cref="IEventAdapter"/> we want to bind to the journal.</typeparam>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <remarks>
        /// This method can be called multiple times for different <see cref="IEventAdapter"/>s.
        /// </remarks>
        public static AkkaConfigurationBuilder AddJournalEventAdapter<TAdapter>(this AkkaConfigurationBuilder builder,
            string journalId, string eventAdapterName, IEnumerable<Type> boundTypes) where TAdapter:IEventAdapter
        {
            Config adapterConfiguration = @$"
            akka.persistence.journal {{
                {journalId} {{
                   
                }}
            }}";
        }
    }
}