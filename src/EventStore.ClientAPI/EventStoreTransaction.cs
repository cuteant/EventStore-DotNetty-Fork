using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    /// <summary>
    /// Represents a multi-request transaction with Event Store
    /// </summary>
    public class EventStoreTransaction : IDisposable
    {
        /// <summary>
        /// The ID of the transaction. This can be used to recover
        /// a transaction later.
        /// </summary>
        public readonly long TransactionId;

        private readonly UserCredentials _userCredentials;
        private readonly IEventStoreTransactionConnection _connection;
        private int _isRolledBack;
        private bool IsRolledBack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Consts.True == Volatile.Read(ref _isRolledBack);
            set => Interlocked.Exchange(ref _isRolledBack, value ? Consts.True : Consts.False);
        }
        private int _isCommitted;
        private bool IsCommitted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Consts.True == Volatile.Read(ref _isCommitted);
            set => Interlocked.Exchange(ref _isCommitted, value ? Consts.True : Consts.False);
        }

        /// <summary>
        /// Constructs a new <see cref="EventStoreTransaction"/>
        /// </summary>
        /// <param name="transactionId">The transaction id of the transaction</param>
        /// <param name="userCredentials">User credentials under which transaction is committed.</param>
        /// <param name="connection">The connection the transaction is hooked to.</param>
        internal EventStoreTransaction(long transactionId, UserCredentials userCredentials, IEventStoreTransactionConnection connection)
        {
            if ((ulong)transactionId > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.transactionId); }

            TransactionId = transactionId;
            _userCredentials = userCredentials;
            _connection = connection;
        }

        /// <summary>
        /// Asynchronously commits this transaction
        /// </summary>
        /// <returns>A <see cref="Task"/> that returns expected version for following write requests</returns>
        public Task<WriteResult> CommitAsync()
        {
            if (IsRolledBack) CoreThrowHelper.ThrowInvalidOperationException_CannotCommitARolledbackTransaction();
            if (IsCommitted) CoreThrowHelper.ThrowInvalidOperationException_TransactionIsAlreadyCommitted();
            IsCommitted = true;
            return _connection.CommitTransactionAsync(this, _userCredentials);
        }

        /// <summary>
        /// Writes to a transaction in Event Store asynchronously
        /// </summary>
        /// <param name="events">The events to write</param>
        /// <returns>A <see cref="Task"/> allowing the caller to control the async operation.</returns>
        public Task WriteAsync(params EventData[] events)
        {
            return WriteAsync((IEnumerable<EventData>)events);
        }

        /// <summary>
        /// Writes to a transaction in Event Store asynchronously
        /// </summary>
        /// <param name="events">The events to write</param>
        /// <returns>A <see cref="Task"/> allowing the caller to control the async operation.</returns>
        public Task WriteAsync(IEnumerable<EventData> events)
        {
            if (IsRolledBack) CoreThrowHelper.ThrowInvalidOperationException_CannotWriteToARolledbackTransaction();
            if (IsCommitted) CoreThrowHelper.ThrowInvalidOperationException_TransactionIsAlreadyCommitted();
            return _connection.TransactionalWriteAsync(this, events);
        }

        /// <summary>
        /// Rollsback this transaction.
        /// </summary>
        public void Rollback()
        {
            if (IsCommitted) CoreThrowHelper.ThrowInvalidOperationException_TransactionIsAlreadyCommitted();
            IsRolledBack = true;
        }

        /// <summary>
        /// Disposes this transaction rolling it back if not already committed
        /// </summary>
        public void Dispose()
        {
            if (!IsCommitted) { IsRolledBack = true; }
        }
    }
}
