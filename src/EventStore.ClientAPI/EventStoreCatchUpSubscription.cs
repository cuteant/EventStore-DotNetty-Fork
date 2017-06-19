using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Serialization;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
  #region --- class EventStoreCatchUpSubscription ---

  /// <summary>Base class representing catch-up subscriptions.</summary>
  public abstract class EventStoreCatchUpSubscription<TSubscription, TResolvedEvent>
    where TSubscription : EventStoreCatchUpSubscription<TSubscription, TResolvedEvent>
    where TResolvedEvent : IResolvedEvent, new()
  {
    #region @@ Fields @@

    private static readonly TResolvedEvent DropSubscriptionEvent = new TResolvedEvent();

    /// <summary>The <see cref="ILogger"/> to use for the subscription.</summary>
    protected static readonly ILogger Log = TraceLogger.GetLogger("EventStore.ClientAPI.CatchUpSubscription");

    private readonly IEventStoreConnection _connection;
    internal readonly UserCredentials _userCredentials;
    private readonly string _streamId;

    /// <summary>The batch size to use during the read phase of the subscription.</summary>
    protected readonly int ReadBatchSize;
    /// <summary>The maximum number of events to buffer before the subscription drops.</summary>
    protected readonly int MaxPushQueueSize;

    /// <summary>Action invoked when a new event appears on the subscription.</summary>
    protected readonly Action<TSubscription, TResolvedEvent> EventAppeared;
    protected readonly Func<TSubscription, TResolvedEvent, Task> EventAppearedAsync;
    private readonly Action<TSubscription> _liveProcessingStarted;
    private readonly Action<TSubscription, SubscriptionDropReason, Exception> _subscriptionDropped;

    /// <summary>Whether or not to use verbose logging (useful during debugging).</summary>
    protected readonly bool Verbose;
    private readonly string _subscriptionName;
    internal readonly CatchUpSubscriptionSettings _settings;

    private readonly int _numActionBlocks;
    internal readonly BufferBlock<TResolvedEvent> _historicalQueue;
    private readonly List<ActionBlock<TResolvedEvent>> _historicalBlocks;
    private IDisposable _historicalLinks;
    private readonly BufferBlock<TResolvedEvent> _liveQueue;
    private readonly List<ActionBlock<TResolvedEvent>> _liveBlocks;
    private IDisposable _liveLinks;
    internal EventStoreSubscription _subscription;
    private DropData _dropData;

    ///<summary>stop has been called.</summary>
    protected volatile bool ShouldStop;
    internal const int c_on = 1;
    internal const int c_off = 0;
    internal int _isDropped = c_off;
    internal Exception _lastHistoricalEventError;
    private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(true);

    #endregion

    #region @@ Properties @@

    /// <summary>Indicates whether the subscription is to all events or to a specific stream.</summary>
    public bool IsSubscribedToAll { get; protected set; } = false;
    /// <summary>The name of the stream to which the subscription is subscribed (empty if subscribed to all).</summary>
    public string StreamId => _streamId;
    /// <summary>The name of subscription.</summary>
    public string SubscriptionName => _subscriptionName;

    /// <summary>Gets the number of items waiting to be processed by this subscription.</summary>
    internal Int32 InputCount { get { return _numActionBlocks == 1 ? _liveBlocks[0].InputCount : _liveQueue.Count; } }

    #endregion

    #region @@ Consturctors @@

    /// <summary>Constructs state for EventStoreCatchUpSubscription.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="streamId">The stream name.</param>
    /// <param name="userCredentials">User credentials for the operations.</param>
    /// <param name="eventAppeared">Action invoked when events are received.</param>
    /// <param name="liveProcessingStarted">Action invoked when the read phase finishes.</param>
    /// <param name="subscriptionDropped">Action invoked if the subscription drops.</param>
    /// <param name="settings">Settings for this subscription.</param>
    protected EventStoreCatchUpSubscription(IEventStoreConnection connection, string streamId, UserCredentials userCredentials,
      Action<TSubscription, TResolvedEvent> eventAppeared, Action<TSubscription> liveProcessingStarted,
      Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped, CatchUpSubscriptionSettings settings)
      : this(connection, streamId, userCredentials, liveProcessingStarted, subscriptionDropped, settings)
    {
      EventAppeared = eventAppeared ?? throw new ArgumentNullException(nameof(eventAppeared));

      _numActionBlocks = settings.NumActionBlocks;
      if (SubscriptionSettings.Unbounded == settings.BoundedCapacityPerBlock)
      {
        // 如果没有设定 ActionBlock 的容量，设置多个 ActionBlock 没有意义
        _numActionBlocks = 1;
      }
      _historicalBlocks = new List<ActionBlock<TResolvedEvent>>(_numActionBlocks);
      _liveBlocks = new List<ActionBlock<TResolvedEvent>>(_numActionBlocks);
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        _historicalBlocks.Add(new ActionBlock<TResolvedEvent>(e => ProcessHistoricalQueue(e), settings.ToExecutionDataflowBlockOptions(true)));
        _liveBlocks.Add(new ActionBlock<TResolvedEvent>(e => ProcessLiveQueue(e), settings.ToExecutionDataflowBlockOptions(true)));
      }
      var links = new CompositeDisposable();
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        links.Add(_historicalQueue.LinkTo(_historicalBlocks[idx]));
      }
      _historicalLinks = links;
    }

    /// <summary>Constructs state for EventStoreCatchUpSubscription.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="streamId">The stream name.</param>
    /// <param name="userCredentials">User credentials for the operations.</param>
    /// <param name="eventAppearedAsync">Action invoked when events are received.</param>
    /// <param name="liveProcessingStarted">Action invoked when the read phase finishes.</param>
    /// <param name="subscriptionDropped">Action invoked if the subscription drops.</param>
    /// <param name="settings">Settings for this subscription.</param>
    protected EventStoreCatchUpSubscription(IEventStoreConnection connection, string streamId, UserCredentials userCredentials,
      Func<TSubscription, TResolvedEvent, Task> eventAppearedAsync, Action<TSubscription> liveProcessingStarted,
      Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped, CatchUpSubscriptionSettings settings)
      : this(connection, streamId, userCredentials, liveProcessingStarted, subscriptionDropped, settings)
    {
      EventAppearedAsync = eventAppearedAsync ?? throw new ArgumentNullException(nameof(eventAppearedAsync));

      _numActionBlocks = settings.NumActionBlocks;
      if (SubscriptionSettings.Unbounded == settings.BoundedCapacityPerBlock)
      {
        // 如果没有设定 ActionBlock 的容量，设置多个 ActionBlock 没有意义
        _numActionBlocks = 1;
      }
      _historicalBlocks = new List<ActionBlock<TResolvedEvent>>(_numActionBlocks);
      _liveBlocks = new List<ActionBlock<TResolvedEvent>>(_numActionBlocks);
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        _historicalBlocks.Add(new ActionBlock<TResolvedEvent>(e => ProcessHistoricalQueueAsync(e), settings.ToExecutionDataflowBlockOptions()));
        _liveBlocks.Add(new ActionBlock<TResolvedEvent>(e => ProcessLiveQueueAsync(e), settings.ToExecutionDataflowBlockOptions()));
      }
      var links = new CompositeDisposable();
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        links.Add(_historicalQueue.LinkTo(_historicalBlocks[idx]));
      }
      _historicalLinks = links;
    }

    private EventStoreCatchUpSubscription(IEventStoreConnection connection, string streamId, UserCredentials userCredentials,
      Action<TSubscription> liveProcessingStarted, Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped, CatchUpSubscriptionSettings settings)
    {
      _connection = connection ?? throw new ArgumentNullException(nameof(connection));

      _streamId = string.IsNullOrEmpty(streamId) ? string.Empty : streamId;
      _settings = settings;
      _userCredentials = userCredentials;
      ReadBatchSize = settings.ReadBatchSize;
      MaxPushQueueSize = settings.MaxLiveQueueSize;

      _liveProcessingStarted = liveProcessingStarted;
      _subscriptionDropped = subscriptionDropped;
      Verbose = settings.VerboseLogging && Log.IsDebugLevelEnabled();
      _subscriptionName = settings.SubscriptionName ?? String.Empty;

      _historicalQueue = new BufferBlock<TResolvedEvent>(settings.ToBufferBlockOptions());
      _liveQueue = new BufferBlock<TResolvedEvent>(settings.ToBufferBlockOptions());
    }

    #endregion

    #region ++ ReadEventsTillAsync ++

    /// <summary>Read events until the given position or event number async.</summary>
    /// <param name="resolveLinkTos">Whether to resolve Link events.</param>
    /// <param name="userCredentials">User credentials for the operation.</param>
    /// <param name="lastCommitPosition">The commit position to read until.</param>
    /// <param name="lastEventNumber">The event number to read until.</param>
    /// <returns></returns>
    protected abstract Task ReadEventsTillAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber);

    #endregion

    #region ++ TryProcess ++

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
    protected abstract void TryProcess(TResolvedEvent e);

    #endregion

    #region ++ TryProcessAsync ++

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
    protected abstract Task TryProcessAsync(TResolvedEvent e);

    #endregion

    #region == StartAsync ==

    internal Task StartAsync()
    {
      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: starting...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
      return RunSubscriptionAsync();
    }

    #endregion

    #region -- Stop --

    /// <summary>Attempts to stop the subscription.</summary>
    /// <param name="timeout">The amount of time within which the subscription should stop.</param>
    /// <exception cref="TimeoutException">Thrown if the subscription fails to stop within it's timeout period.</exception>
    public void Stop(TimeSpan timeout)
    {
      Stop();
      if (Verbose) Log.LogDebug("Waiting on subscription {0} to stop", SubscriptionName);
      if (!_stopped.Wait(timeout))
      {
        throw new TimeoutException(string.Format("Could not stop {0} in time.", GetType().Name));
      }

      _historicalQueue?.Complete();
      _historicalLinks?.Dispose();
      foreach (var block in _historicalBlocks)
      {
        block?.Complete();
      }

      _liveQueue?.Complete();
      _liveLinks?.Dispose();
      foreach (var block in _liveBlocks)
      {
        block?.Complete();
      }
    }

    /// <summary>Attempts to stop the subscription without blocking for completion of stop.</summary>
    public void Stop()
    {
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: requesting stop...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        Log.LogDebug("Catch-up Subscription {0} to {1}: unhooking from connection.Connected.", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
      }
      _connection.Connected -= OnReconnect;

      ShouldStop = true;
      EnqueueSubscriptionDropNotification(SubscriptionDropReason.UserInitiated, null);
    }

    #endregion

    #region ** OnReconnect **

    private void OnReconnect(object sender, ClientConnectionEventArgs clientConnectionEventArgs)
    {
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: recovering after reconnection.", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        Log.LogDebug("Catch-up Subscription {0} to {1}: unhooking from connection.Connected.", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
      }
      _connection.Connected -= OnReconnect;
      RunSubscriptionAsync();
    }

    #endregion

    #region ** RunSubscriptionAsync **

    private Task RunSubscriptionAsync() => LoadHistoricalEventsAsync();

    #endregion

    #region ** LoadHistoricalEventsAsync **

    private async Task LoadHistoricalEventsAsync()
    {
      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: running...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

      _stopped.Reset();
      var link = Interlocked.Exchange(ref _liveLinks, null);
      link?.Dispose();

      // 等待所有事件都被消费完毕
      var spinner = new SpinWait();
      while (_liveBlocks != null && _liveBlocks.Any(_ => _.InputCount > 0))
      {
        spinner.SpinOnce();
      }

      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: pulling events...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        try
        {
          await ReadEventsTillAsync(_settings.ResolveLinkTos, _userCredentials, null, null).ConfigureAwait(false);
          await SubscribeToStreamAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          DropSubscription(SubscriptionDropReason.CatchUpError, ex);
          throw;
        }
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }

    #endregion

    #region ++ SubscribeToStreamAsync ++

    protected abstract Task SubscribeToStreamAsync();

    #endregion

    #region ++ ReadMissedHistoricEventsAsync ++

    protected async Task ReadMissedHistoricEventsAsync()
    {
      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: pulling events (if left)...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        await ReadEventsTillAsync(_settings.ResolveLinkTos, _userCredentials, _subscription.LastCommitPosition, _subscription.LastEventNumber).ConfigureAwait(false);
        StartLiveProcessing();
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }

    #endregion

    #region ** StartLiveProcessing **

    private void StartLiveProcessing()
    {
      // 等待所有历史事件都被消费完毕
      var spinner = new SpinWait();
      while ((_historicalQueue != null && _historicalQueue.Count > 0) ||
             (_historicalBlocks != null && _historicalBlocks.Any(_ => _.InputCount > 0)))
      {
        spinner.SpinOnce();
      }

      if (ShouldStop)
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
        return;
      }

      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: processing live events...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

      _liveProcessingStarted?.Invoke(this as TSubscription);

      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: hooking to connection.Connected", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
      _connection.Connected += OnReconnect;

      var links = new CompositeDisposable();
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        links.Add(_liveQueue.LinkTo(_liveBlocks[idx]));
      }

      Interlocked.Exchange(ref _liveLinks, links);
    }

    #endregion

    #region ++ EnqueuePushedEventAsync ++

    protected async Task EnqueuePushedEventAsync(EventStoreSubscription subscription, TResolvedEvent e)
    {
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: event appeared ({2}, {3}, {4} @ {5}).",
                  SubscriptionName,
                  IsSubscribedToAll ? "<all>" : StreamId,
                  e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEventType, e.OriginalPosition);
      }

      if (InputCount >= MaxPushQueueSize)
      {
        EnqueueSubscriptionDropNotification(SubscriptionDropReason.ProcessingQueueOverflow, null);
        subscription.Unsubscribe();
        return;
      }

      await _liveQueue.SendAsync(e).ConfigureAwait(false);
    }

    #endregion

    #region ++ ServerSubscriptionDropped ++

    protected void ServerSubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exc)
    {
      EnqueueSubscriptionDropNotification(reason, exc);
    }

    #endregion

    #region ** EnqueueSubscriptionDropNotification **

    private void EnqueueSubscriptionDropNotification(SubscriptionDropReason reason, Exception error)
    {
      // if drop data was already set -- no need to enqueue drop again, somebody did that already
      var dropData = new DropData(reason, error);
      if (Interlocked.CompareExchange(ref _dropData, dropData, null) == null)
      {
        _liveQueue.SendAsync(DropSubscriptionEvent).ConfigureAwait(false).GetAwaiter().GetResult();
      }
    }

    #endregion

    #region ** ProcessLiveQueue **

    private void ProcessLiveQueue(TResolvedEvent e)
    {
      if (e.Equals(DropSubscriptionEvent)) // drop subscription artificial ResolvedEvent
      {
        if (_dropData == null)
        {
          _dropData = new DropData(SubscriptionDropReason.Unknown, new Exception("Drop reason not specified."));
        }
        DropSubscription(_dropData.Reason, _dropData.Error);
        return;
      }
      try
      {
        TryProcess(e);
      }
      catch (Exception exc)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1} Exception occurred in subscription {1}", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, exc);
        DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
      }
    }

    #endregion

    #region ** ProcessLiveQueueAsync **

    private async Task ProcessLiveQueueAsync(TResolvedEvent e)
    {
      if (e.Equals(DropSubscriptionEvent)) // drop subscription artificial ResolvedEvent
      {
        if (_dropData == null)
        {
          _dropData = new DropData(SubscriptionDropReason.Unknown, new Exception("Drop reason not specified."));
        }
        DropSubscription(_dropData.Reason, _dropData.Error);
        return;
      }
      try
      {
        await TryProcessAsync(e).ConfigureAwait(false);
      }
      catch (Exception exc)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1} Exception occurred in subscription {1}", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, exc);
        DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
      }
    }

    #endregion

    #region ** ProcessHistoricalQueue **

    private void ProcessHistoricalQueue(TResolvedEvent e)
    {
      if (_lastHistoricalEventError != null) { return; }
      try
      {
        TryProcess(e);
      }
      catch (Exception exc)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1} Exception occurred in subscription {1}", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, exc);
        Interlocked.Exchange(ref _lastHistoricalEventError, exc);
        DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
      }
    }

    #endregion

    #region ** ProcessHistoricalQueueAsync **

    private async Task ProcessHistoricalQueueAsync(TResolvedEvent e)
    {
      if (_lastHistoricalEventError != null) { return; }
      try
      {
        await TryProcessAsync(e).ConfigureAwait(false);
      }
      catch (Exception exc)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1} Exception occurred in subscription {1}", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, exc);
        Interlocked.Exchange(ref _lastHistoricalEventError, exc);
        DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
      }
    }

    #endregion

    #region ** DropSubscription **

    internal void DropSubscription(SubscriptionDropReason reason, Exception error)
    {
      if (Interlocked.CompareExchange(ref _isDropped, c_on, c_off) == c_off)
      {
        if (Verbose)
        {
          Log.LogDebug("Catch-up Subscription {0} to {1}: dropping subscription, reason: {2} {3}.",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId,
                    reason, error == null ? string.Empty : error.ToString());
        }

        _subscription?.Unsubscribe();
        _subscriptionDropped?.Invoke(this as TSubscription, reason, error);
        _stopped.Set();
      }
    }

    #endregion

    #region ** class DropData **

    private sealed class DropData
    {
      public readonly SubscriptionDropReason Reason;
      public readonly Exception Error;

      public DropData(SubscriptionDropReason reason, Exception error)
      {
        Reason = reason;
        Error = error;
      }
    }

    #endregion
  }

  #endregion

  #region --- class EventStoreAllCatchUpSubscription ---

  /// <summary>A catch-up subscription to all events in the Event Store.</summary>
  public class EventStoreAllCatchUpSubscription : EventStoreCatchUpSubscription<EventStoreAllCatchUpSubscription, ResolvedEvent>
  {
    /// <summary>The last position processed on the subscription.</summary>
    public Position LastProcessedPosition
    {
      get
      {
        Position oldPos = _lastProcessedPosition;
        Position curPos;
        while (oldPos != (curPos = _lastProcessedPosition))
        {
          oldPos = curPos;
        }
        return curPos;
      }
    }

    private Position _nextReadPosition;
    private Position _lastProcessedPosition;
    private readonly IEventStoreConnection _innerConnection;

    internal EventStoreAllCatchUpSubscription(IEventStoreConnection connection,
                                                  Position? fromPositionExclusive, /* if null -- from the very beginning */
                                                  UserCredentials userCredentials,
                                                  Action<EventStoreAllCatchUpSubscription, ResolvedEvent> eventAppeared,
                                                  Action<EventStoreAllCatchUpSubscription> liveProcessingStarted,
                                                  Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                  CatchUpSubscriptionSettings settings)
      : base(connection, string.Empty, userCredentials, eventAppeared, liveProcessingStarted, subscriptionDropped, settings)
    {
      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
      IsSubscribedToAll = true;
      _lastProcessedPosition = fromPositionExclusive ?? new Position(-1, -1);
      _nextReadPosition = fromPositionExclusive ?? Position.Start;
    }

    internal EventStoreAllCatchUpSubscription(IEventStoreConnection connection,
                                                  Position? fromPositionExclusive, /* if null -- from the very beginning */
                                                  UserCredentials userCredentials,
                                                  Func<EventStoreAllCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
                                                  Action<EventStoreAllCatchUpSubscription> liveProcessingStarted,
                                                  Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                  CatchUpSubscriptionSettings settings)
      : base(connection, string.Empty, userCredentials, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, settings)
    {
      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
      IsSubscribedToAll = true;
      _lastProcessedPosition = fromPositionExclusive ?? new Position(-1, -1);
      _nextReadPosition = fromPositionExclusive ?? Position.Start;
    }

    /// <inheritdoc />
    protected override Task ReadEventsTillAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
        => ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber);

    private async Task ReadEventsInternalAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      var slice = await _innerConnection.ReadAllEventsForwardAsync(_nextReadPosition, ReadBatchSize, resolveLinkTos, userCredentials).ConfigureAwait(false);
      await ReadEventsCallbackAsync(slice, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
    }

    private async Task ReadEventsCallbackAsync(AllEventsSlice slice, bool resolveLinkTos, UserCredentials userCredentials,
      long? lastCommitPosition, long? lastEventNumber)
    {
      if (!(await ProcessEventsAsync(lastCommitPosition, slice).ConfigureAwait(false)) && !ShouldStop)
      {
        await ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
      }
      else
      {
        if (Verbose)
        {
          Log.LogDebug("Catch-up Subscription {0} to {1}: finished reading events, nextReadPosition = {2}.",
              SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, _nextReadPosition);
        }
      }
    }

    protected override async Task SubscribeToStreamAsync()
    {
      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: subscribing...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        var subscription = await _innerConnection.SubscribeToAllAsync(
            _settings.ResolveLinkTos ? SubscriptionSettings.ResolveLinkTosSettings : SubscriptionSettings.Default,
            eventAppearedAsync: EnqueuePushedEventAsync,
            subscriptionDropped: ServerSubscriptionDropped,
            userCredentials: _userCredentials).ConfigureAwait(false);

        _subscription = subscription;
        await ReadMissedHistoricEventsAsync().ConfigureAwait(false);
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }

    private async Task<bool> ProcessEventsAsync(long? lastCommitPosition, AllEventsSlice slice)
    {
      foreach (var e in slice.Events)
      {
        if (e.OriginalPosition == null) throw new Exception($"Subscription {SubscriptionName} event came up with no OriginalPosition.");
        if (null == _lastHistoricalEventError)
        {
          await _historicalQueue.SendAsync(e).ConfigureAwait(false);
        }
        else
        {
          throw _lastHistoricalEventError;
        }
      }
      //if (EventAppeared != null)
      //{
      //  foreach (var e in slice.Events)
      //  {
      //    if (e.OriginalPosition == null) throw new Exception($"Subscription {SubscriptionName} event came up with no OriginalPosition.");
      //    TryProcess(e);
      //  }
      //}
      //else
      //{
      //  foreach (var e in slice.Events)
      //  {
      //    if (e.OriginalPosition == null) throw new Exception($"Subscription {SubscriptionName} event came up with no OriginalPosition.");
      //    await TryProcessAsync(e).ConfigureAwait(false);
      //  }
      //}

      _nextReadPosition = slice.NextPosition;

      var done = lastCommitPosition == null
          ? slice.IsEndOfStream
          : slice.NextPosition >= new Position(lastCommitPosition.Value, lastCommitPosition.Value);

      if (!done && slice.IsEndOfStream)
      {
        // we are waiting for server to flush its data
        //Thread.Sleep(1); 
        var spinner = new SpinWait();
        spinner.SpinOnce();
      }
      return done;
    }

    /// <inheritdoc />
    protected override void TryProcess(ResolvedEvent e)
    {
      bool processed = false;
      if (e.OriginalPosition > _lastProcessedPosition)
      {
        try
        {
          EventAppeared(this, e);
        }
        catch (Exception ex)
        {
          DropSubscription(SubscriptionDropReason.EventHandlerException, ex);
          throw;
        }
        _lastProcessedPosition = e.OriginalPosition.Value;
        processed = true;
      }
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId,
                    processed ? "processed" : "skipping",
                    e.OriginalEvent.EventStreamId, e.OriginalEvent.EventNumber, e.OriginalEvent.EventType, e.OriginalPosition);
      }
    }

    /// <inheritdoc />
    protected override async Task TryProcessAsync(ResolvedEvent e)
    {
      bool processed = false;
      if (e.OriginalPosition > _lastProcessedPosition)
      {
        try
        {
          await EventAppearedAsync(this, e).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          DropSubscription(SubscriptionDropReason.EventHandlerException, ex);
          throw;
        }
        _lastProcessedPosition = e.OriginalPosition.Value;
        processed = true;
      }
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId,
                    processed ? "processed" : "skipping",
                    e.OriginalEvent.EventStreamId, e.OriginalEvent.EventNumber, e.OriginalEvent.EventType, e.OriginalPosition);
      }
    }
  }

  #endregion

  #region --- class EventStoreStreamCatchUpSubscriptionBase ---

  /// <summary>A catch-up subscription to a single stream in the Event Store.</summary>
  public abstract class EventStoreStreamCatchUpSubscriptionBase<TSubscription, TStreamEventsSlice, TResolvedEvent> : EventStoreCatchUpSubscription<TSubscription, TResolvedEvent>
    where TSubscription : EventStoreStreamCatchUpSubscriptionBase<TSubscription, TStreamEventsSlice, TResolvedEvent>
    where TStreamEventsSlice : IStreamEventsSlice<TResolvedEvent>
    where TResolvedEvent : IResolvedEvent, new()
  {
    /// <summary>The last event number processed on the subscription.</summary>
    public long LastProcessedEventNumber => _lastProcessedEventNumber;

    protected long _nextReadEventNumber;
    protected long _lastProcessedEventNumber;

    internal EventStoreStreamCatchUpSubscriptionBase(IEventStoreConnection connection,
                                                     string streamId,
                                                     long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                                     UserCredentials userCredentials,
                                                     Action<TSubscription, TResolvedEvent> eventAppeared,
                                                     Action<TSubscription> liveProcessingStarted,
                                                     Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                     CatchUpSubscriptionSettings settings)
      : base(connection, streamId, userCredentials, eventAppeared, liveProcessingStarted, subscriptionDropped, settings)
    {
      Ensure.NotNullOrEmpty(streamId, nameof(streamId));

      _lastProcessedEventNumber = fromEventNumberExclusive ?? -1;
      _nextReadEventNumber = fromEventNumberExclusive ?? 0;
    }

    internal EventStoreStreamCatchUpSubscriptionBase(IEventStoreConnection connection,
                                                     string streamId,
                                                     long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                                     UserCredentials userCredentials,
                                                     Func<TSubscription, TResolvedEvent, Task> eventAppearedAsync,
                                                     Action<TSubscription> liveProcessingStarted,
                                                     Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                     CatchUpSubscriptionSettings settings)
      : base(connection, streamId, userCredentials, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, settings)
    {
      Ensure.NotNullOrEmpty(streamId, nameof(streamId));

      _lastProcessedEventNumber = fromEventNumberExclusive ?? -1;
      _nextReadEventNumber = fromEventNumberExclusive ?? 0;
    }
    protected async Task<bool> ProcessEventsAsync(long? lastEventNumber, TStreamEventsSlice slice)
    {
      bool done;
      switch (slice.Status)
      {
        case SliceReadStatus.Success:
          {
            foreach (var e in slice.Events)
            {
              if (null == _lastHistoricalEventError)
              {
                await _historicalQueue.SendAsync(e).ConfigureAwait(false);
              }
              else
              {
                throw _lastHistoricalEventError;
              }
            }
            //if (EventAppeared != null)
            //{
            //  foreach (var e in slice.Events)
            //  {
            //    TryProcess(e);
            //  }
            //}
            //else
            //{
            //  foreach (var e in slice.Events)
            //  {
            //    await TryProcessAsync(e).ConfigureAwait(false);
            //  }
            //}
            Interlocked.Exchange(ref _nextReadEventNumber, slice.NextEventNumber);
            done = lastEventNumber == null ? slice.IsEndOfStream : slice.NextEventNumber > lastEventNumber;
            break;
          }
        case SliceReadStatus.StreamNotFound:
          {
            if (lastEventNumber.HasValue && lastEventNumber != -1)
            {
              throw new Exception($"Impossible: stream {StreamId} disappeared in the middle of catching up subscription {SubscriptionName}.");
            }
            done = true;
            break;
          }
        case SliceReadStatus.StreamDeleted:
          throw new StreamDeletedException(StreamId);
        default:
          throw new ArgumentOutOfRangeException($"Subscription {SubscriptionName} unexpected StreamEventsSlice.Status: {SubscriptionName}.");
      }

      if (!done && slice.IsEndOfStream)
      {
        // we are waiting for server to flush its data
        //Thread.Sleep(1); 
        var spinner = new SpinWait();
        spinner.SpinOnce();
      }
      return done;
    }

    /// <inheritdoc />
    protected override void TryProcess(TResolvedEvent e)
    {
      bool processed = false;
      if (e.OriginalEventNumber > _lastProcessedEventNumber)
      {
        try
        {
          EventAppeared(this as TSubscription, e);
        }
        catch (Exception ex)
        {
          DropSubscription(SubscriptionDropReason.EventHandlerException, ex);
          throw;
        }
        Interlocked.Exchange(ref _lastProcessedEventNumber, e.OriginalEventNumber);
        processed = true;
      }
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId, processed ? "processed" : "skipping",
                    e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEventType, e.OriginalEventNumber);
      }
    }

    /// <inheritdoc />
    protected override async Task TryProcessAsync(TResolvedEvent e)
    {
      bool processed = false;
      if (e.OriginalEventNumber > _lastProcessedEventNumber)
      {
        try
        {
          await EventAppearedAsync(this as TSubscription, e).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          DropSubscription(SubscriptionDropReason.EventHandlerException, ex);
          throw;
        }
        Interlocked.Exchange(ref _lastProcessedEventNumber, e.OriginalEventNumber);
        processed = true;
      }
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId, processed ? "processed" : "skipping",
                    e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEventType, e.OriginalEventNumber);
      }
    }
  }

  #endregion

  #region --- class EventStoreStreamCatchUpSubscription ---

  /// <summary>A catch-up subscription to a single stream in the Event Store.</summary>
  public class EventStoreStreamCatchUpSubscription : EventStoreStreamCatchUpSubscriptionBase<EventStoreStreamCatchUpSubscription, StreamEventsSlice, ResolvedEvent>
  {
    private readonly IEventStoreConnection _innerConnection;

    internal EventStoreStreamCatchUpSubscription(IEventStoreConnection connection,
                                                     string streamId,
                                                     long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                                     UserCredentials userCredentials,
                                                     Action<EventStoreStreamCatchUpSubscription, ResolvedEvent> eventAppeared,
                                                     Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted,
                                                     Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                     CatchUpSubscriptionSettings settings)
      : base(connection, streamId, fromEventNumberExclusive, userCredentials, eventAppeared, liveProcessingStarted, subscriptionDropped, settings)
    {

      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    internal EventStoreStreamCatchUpSubscription(IEventStoreConnection connection,
                                                     string streamId,
                                                     long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                                     UserCredentials userCredentials,
                                                     Func<EventStoreStreamCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
                                                     Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted,
                                                     Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                     CatchUpSubscriptionSettings settings)
      : base(connection, streamId, fromEventNumberExclusive, userCredentials, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, settings)
    {
      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    protected override Task ReadEventsTillAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
        => ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber);

    private async Task ReadEventsInternalAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      var slice = await _innerConnection.ReadStreamEventsForwardAsync(StreamId, _nextReadEventNumber, ReadBatchSize, resolveLinkTos, userCredentials).ConfigureAwait(false);
      await ReadEventsCallbackAsync(slice, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
    }

    private async Task ReadEventsCallbackAsync(StreamEventsSlice slice, bool resolveLinkTos, UserCredentials userCredentials,
      long? lastCommitPosition, long? lastEventNumber)
    {
      if (!(await ProcessEventsAsync(lastEventNumber, slice).ConfigureAwait(false)) && !ShouldStop)
      {
        await ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
      }
      else
      {
        if (Verbose)
        {
          Log.LogDebug("Catch-up Subscription {0} to {1}: finished reading events, nextReadEventNumber = {2}.",
              SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, _nextReadEventNumber);
        }
      }
    }

    protected override async Task SubscribeToStreamAsync()
    {
      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: subscribing...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        var subscription = await _innerConnection.SubscribeToStreamAsync(StreamId,
            _settings.ResolveLinkTos ? SubscriptionSettings.ResolveLinkTosSettings : SubscriptionSettings.Default,
            eventAppearedAsync: EnqueuePushedEventAsync,
            subscriptionDropped: ServerSubscriptionDropped,
            userCredentials: _userCredentials).ConfigureAwait(false);

        _subscription = subscription;
        await ReadMissedHistoricEventsAsync().ConfigureAwait(false);
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }
  }

  #endregion

  #region --- class EventStoreCatchUpSubscription ---

  /// <summary>A catch-up subscription to a single stream in the Event Store.</summary>
  public class EventStoreCatchUpSubscription : EventStoreStreamCatchUpSubscriptionBase<EventStoreCatchUpSubscription, StreamEventsSlice<object>, ResolvedEvent<object>>
  {
    private readonly IEventStoreConnection2 _innerConnection;

    internal EventStoreCatchUpSubscription(IEventStoreConnection2 connection,
                                               string streamId,
                                               long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                               UserCredentials userCredentials,
                                               Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared,
                                               Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                               Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                               CatchUpSubscriptionSettings settings)
      : base(connection, streamId, fromEventNumberExclusive, userCredentials, eventAppeared, liveProcessingStarted, subscriptionDropped, settings)
    {
      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    internal EventStoreCatchUpSubscription(IEventStoreConnection2 connection,
                                               string streamId,
                                               long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                               UserCredentials userCredentials,
                                               Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
                                               Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                               Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                               CatchUpSubscriptionSettings settings)
      : base(connection, streamId, fromEventNumberExclusive, userCredentials, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, settings)
    {
      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    protected override Task ReadEventsTillAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
        => ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber);

    private async Task ReadEventsInternalAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      var slice = await _innerConnection.GetStreamEventsForwardAsync(StreamId, _nextReadEventNumber, ReadBatchSize, resolveLinkTos, userCredentials).ConfigureAwait(false);
      await ReadEventsCallbackAsync(slice, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
    }

    private async Task ReadEventsCallbackAsync(StreamEventsSlice<object> slice, bool resolveLinkTos, UserCredentials userCredentials,
      long? lastCommitPosition, long? lastEventNumber)
    {
      if (!(await ProcessEventsAsync(lastEventNumber, slice).ConfigureAwait(false)) && !ShouldStop)
      {
        await ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
      }
      else
      {
        if (Verbose)
        {
          Log.LogDebug("Catch-up Subscription {0} to {1}: finished reading events, nextReadEventNumber = {2}.",
              SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, _nextReadEventNumber);
        }
      }
    }

    protected override async Task SubscribeToStreamAsync()
    {
      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: subscribing...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        var subscription = await _innerConnection.VolatileSubscribeAsync(StreamId,
            _settings.ResolveLinkTos ? SubscriptionSettings.ResolveLinkTosSettings : SubscriptionSettings.Default,
            eventAppearedAsync: EnqueuePushedEventAsync,
            subscriptionDropped: ServerSubscriptionDropped,
            userCredentials: _userCredentials).ConfigureAwait(false);

        _subscription = subscription;
        await ReadMissedHistoricEventsAsync().ConfigureAwait(false);
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }
  }

  #endregion

  #region --- class EventStoreCatchUpSubscription<TEvent> ---

  /// <summary>A catch-up subscription to a single stream in the Event Store.</summary>
  public class EventStoreCatchUpSubscription<TEvent> : EventStoreStreamCatchUpSubscriptionBase<EventStoreCatchUpSubscription<TEvent>, StreamEventsSlice<TEvent>, ResolvedEvent<TEvent>>
    where TEvent : class
  {
    private readonly IEventStoreConnection2 _innerConnection;
    private readonly string _topic;

    internal EventStoreCatchUpSubscription(IEventStoreConnection2 connection,
                                               string topic,
                                               long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                               UserCredentials userCredentials,
                                               Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared,
                                               Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted,
                                               Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped,
                                               CatchUpSubscriptionSettings settings)
      : base(connection, string.IsNullOrWhiteSpace(topic) ? SerializationManager.GetStreamId(typeof(TEvent)) : IEventStoreConnectionExtensions.CombineStreamId(SerializationManager.GetStreamId(typeof(TEvent)), topic),
          fromEventNumberExclusive, userCredentials, eventAppeared, liveProcessingStarted, subscriptionDropped, settings)
    {
      _topic = topic;
      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    internal EventStoreCatchUpSubscription(IEventStoreConnection2 connection,
                                               string topic,
                                               long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                               UserCredentials userCredentials,
                                               Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
                                               Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted,
                                               Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped,
                                               CatchUpSubscriptionSettings settings)
      : base(connection, string.IsNullOrWhiteSpace(topic) ? SerializationManager.GetStreamId(typeof(TEvent)) : IEventStoreConnectionExtensions.CombineStreamId(SerializationManager.GetStreamId(typeof(TEvent)), topic),
          fromEventNumberExclusive, userCredentials, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, settings)
    {
      _topic = topic;
      _innerConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    protected override Task ReadEventsTillAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
        => ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber);

    private async Task ReadEventsInternalAsync(bool resolveLinkTos, UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      var slice = string.IsNullOrWhiteSpace(_topic)
                ? await _innerConnection.GetStreamEventsForwardAsync<TEvent>(_nextReadEventNumber, ReadBatchSize, resolveLinkTos, userCredentials).ConfigureAwait(false)
                : await _innerConnection.GetStreamEventsForwardAsync<TEvent>(_topic, _nextReadEventNumber, ReadBatchSize, resolveLinkTos, userCredentials).ConfigureAwait(false);
      await ReadEventsCallbackAsync(slice, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
    }

    private async Task ReadEventsCallbackAsync(StreamEventsSlice<TEvent> slice, bool resolveLinkTos, UserCredentials userCredentials,
      long? lastCommitPosition, long? lastEventNumber)
    {
      if (!(await ProcessEventsAsync(lastEventNumber, slice).ConfigureAwait(false)) && !ShouldStop)
      {
        await ReadEventsInternalAsync(resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
      }
      else
      {
        if (Verbose)
        {
          Log.LogDebug("Catch-up Subscription {0} to {1}: finished reading events, nextReadEventNumber = {2}.",
              SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, _nextReadEventNumber);
        }
      }
    }

    protected override async Task SubscribeToStreamAsync()
    {
      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: subscribing...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        var subscription = string.IsNullOrWhiteSpace(_topic)
            ? await _innerConnection.VolatileSubscribeAsync<TEvent>(
                    _settings.ResolveLinkTos ? SubscriptionSettings.ResolveLinkTosSettings : SubscriptionSettings.Default,
                    eventAppearedAsync: EnqueuePushedEventAsync,
                    subscriptionDropped: ServerSubscriptionDropped,
                    userCredentials: _userCredentials).ConfigureAwait(false)
            : await _innerConnection.VolatileSubscribeAsync<TEvent>(_topic,
                    _settings.ResolveLinkTos ? SubscriptionSettings.ResolveLinkTosSettings : SubscriptionSettings.Default,
                    eventAppearedAsync: EnqueuePushedEventAsync,
                    subscriptionDropped: ServerSubscriptionDropped,
                    userCredentials: _userCredentials).ConfigureAwait(false);

        _subscription = subscription;
        await ReadMissedHistoricEventsAsync().ConfigureAwait(false);
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }
  }

  #endregion
}
