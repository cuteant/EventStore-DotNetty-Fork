using System;
using System.Collections.Generic;
using CuteAnt.AsyncEx;

namespace EventStore.ClientAPI
{
  /// <summary>EventStoreTransactionExtensions</summary>
  public static partial class EventStoreTransactionExtensions
  {
    /// <summary>Commits this transaction.</summary>
    /// <param name="transaction">The <see cref="EventStoreTransaction"/> to write to.</param>
    /// <returns>A expected version for following write requests</returns>
    public static WriteResult Commit(this EventStoreTransaction transaction)
    {
      if (null == @transaction) { throw new ArgumentNullException(nameof(transaction)); }

      return AsyncContext.Run(
                async trans => await trans.CommitAsync().ConfigureAwait(false),
                transaction);
    }

    /// <summary>Writes to a transaction in Event Store.</summary>
    /// <param name="transaction">The <see cref="EventStoreTransaction"/> to write to.</param>
    /// <param name="events">The events to write</param>
    public static void Write(this EventStoreTransaction transaction, params EventData[] events)
    {
      if (null == transaction) { throw new ArgumentNullException(nameof(transaction)); }

      AsyncContext.Run(
          async (trans, es) => await trans.WriteAsync(es).ConfigureAwait(false),
          transaction, events);
    }

    /// <summary>Writes to a transaction in Event Store.</summary>
    /// <param name="transaction">The <see cref="EventStoreTransaction"/> to write to.</param>
    /// <param name="events">The events to write</param>
    public static void Write(this EventStoreTransaction transaction, IEnumerable<EventData> events)
    {
      if (null == transaction) { throw new ArgumentNullException(nameof(transaction)); }

      AsyncContext.Run(
          async (trans, es) => await trans.WriteAsync(es).ConfigureAwait(false),
          transaction, events);
    }
  }
}
