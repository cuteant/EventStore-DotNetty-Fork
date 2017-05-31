﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
  /// <summary>Base class representing catch-up subscriptions.</summary>
  public abstract class EventStoreCatchUpSubscription
  {
    private static readonly ResolvedEvent DropSubscriptionEvent = new ResolvedEvent();

    /// <summary>Indicates whether the subscription is to all events or to a specific stream.</summary>
    public bool IsSubscribedToAll => _streamId == string.Empty;
    /// <summary>The name of the stream to which the subscription is subscribed (empty if subscribed to all).</summary>
    public string StreamId => _streamId;
    /// <summary>The name of subscription.</summary>
    public string SubscriptionName => _subscriptionName;

    /// <summary>The <see cref="ILogger"/> to use for the subscription.</summary>
    protected readonly ILogger Log;

    private readonly IEventStoreConnection _connection;
    private readonly bool _resolveLinkTos;
    private readonly UserCredentials _userCredentials;
    private readonly string _streamId;

    /// <summary>The batch size to use during the read phase of the subscription.</summary>
    protected readonly int ReadBatchSize;
    /// <summary>The maximum number of events to buffer before the subscription drops.</summary>
    protected readonly int MaxPushQueueSize;

    /// <summary>Action invoked when a new event appears on the subscription.</summary>
    protected readonly Action<EventStoreCatchUpSubscription, ResolvedEvent> EventAppeared;
    protected readonly Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> EventAppearedAsync;
    private readonly Action<EventStoreCatchUpSubscription> _liveProcessingStarted;
    private readonly Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> _subscriptionDropped;

    /// <summary>Whether or not to use verbose logging (useful during debugging).</summary>
    protected readonly bool Verbose;
    private readonly string _subscriptionName;

    private readonly BufferBlock<ResolvedEvent> _liveQueue;
    private readonly int _numActionBlocks;
    private readonly List<ActionBlock<ResolvedEvent>> _actionBlocks;
    private IDisposable _links;
    private EventStoreSubscription _subscription;
    private DropData _dropData;

    ///<summary>stop has been called.</summary>
    protected volatile bool ShouldStop;
    private int _isDropped;
    private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(true);

    /// <summary>Gets the number of items waiting to be processed by this subscription.</summary>
    internal Int32 InputCount { get { return _numActionBlocks == 1 ? _actionBlocks[0].InputCount : _liveQueue.Count; } }

    /// <summary>Read events until the given position or event number async.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events.</param>
    /// <param name="userCredentials">User credentials for the operation.</param>
    /// <param name="lastCommitPosition">The commit position to read until.</param>
    /// <param name="lastEventNumber">The event number to read until.</param>
    /// <returns></returns>
    protected abstract Task ReadEventsTillAsync(IEventStoreConnection connection,
                                                   bool resolveLinkTos,
                                                   UserCredentials userCredentials,
                                                   long? lastCommitPosition,
                                                   long? lastEventNumber);

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
    protected abstract void TryProcess(ResolvedEvent e);

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
    protected abstract Task TryProcessAsync(ResolvedEvent e);

    /// <summary>Constructs state for EventStoreCatchUpSubscription.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="streamId">The stream name.</param>
    /// <param name="userCredentials">User credentials for the operations.</param>
    /// <param name="eventAppeared">Action invoked when events are received.</param>
    /// <param name="liveProcessingStarted">Action invoked when the read phase finishes.</param>
    /// <param name="subscriptionDropped">Action invoked if the subscription drops.</param>
    /// <param name="settings">Settings for this subscription.</param>
    protected EventStoreCatchUpSubscription(IEventStoreConnection connection,
                                                string streamId,
                                                UserCredentials userCredentials,
                                                Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
                                                Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                                Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                CatchUpSubscriptionSettings settings)
      : this(connection, streamId, userCredentials, liveProcessingStarted, subscriptionDropped, settings)
    {
      EventAppeared = eventAppeared ?? throw new ArgumentNullException(nameof(eventAppeared));

      _numActionBlocks = settings.NumActionBlocks;
      if (SubscriptionSettings.Unbounded == settings.BoundedCapacityPerBlock)
      {
        // 如果没有设定 ActionBlock 的容量，设置多个 ActionBlock 没有意义
        _numActionBlocks = 1;
      }
      _actionBlocks = new List<ActionBlock<ResolvedEvent>>(_numActionBlocks);
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        _actionBlocks.Add(new ActionBlock<ResolvedEvent>(e => ProcessLiveQueue(e), settings.ToExecutionDataflowBlockOptions(true)));
      }
    }

    /// <summary>Constructs state for EventStoreCatchUpSubscription.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="streamId">The stream name.</param>
    /// <param name="userCredentials">User credentials for the operations.</param>
    /// <param name="eventAppearedAsync">Action invoked when events are received.</param>
    /// <param name="liveProcessingStarted">Action invoked when the read phase finishes.</param>
    /// <param name="subscriptionDropped">Action invoked if the subscription drops.</param>
    /// <param name="settings">Settings for this subscription.</param>
    protected EventStoreCatchUpSubscription(IEventStoreConnection connection,
                                                string streamId,
                                                UserCredentials userCredentials,
                                                Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
                                                Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                                Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                CatchUpSubscriptionSettings settings)
      : this(connection, streamId, userCredentials, liveProcessingStarted, subscriptionDropped, settings)
    {
      EventAppearedAsync = eventAppearedAsync ?? throw new ArgumentNullException(nameof(eventAppearedAsync));

      _numActionBlocks = settings.NumActionBlocks;
      if (SubscriptionSettings.Unbounded == settings.BoundedCapacityPerBlock)
      {
        // 如果没有设定 ActionBlock 的容量，设置多个 ActionBlock 没有意义
        _numActionBlocks = 1;
      }
      _actionBlocks = new List<ActionBlock<ResolvedEvent>>(_numActionBlocks);
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        _actionBlocks.Add(new ActionBlock<ResolvedEvent>(e => ProcessLiveQueueAsync(e), settings.ToExecutionDataflowBlockOptions()));
      }
    }

    private EventStoreCatchUpSubscription(IEventStoreConnection connection,
                                              string streamId,
                                              UserCredentials userCredentials,
                                              Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                              Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                              CatchUpSubscriptionSettings settings)
    {
      _connection = connection ?? throw new ArgumentNullException(nameof(connection));
      Log = TraceLogger.GetLogger(this.GetType());
      _streamId = string.IsNullOrEmpty(streamId) ? string.Empty : streamId;
      _resolveLinkTos = settings.ResolveLinkTos;
      _userCredentials = userCredentials;
      ReadBatchSize = settings.ReadBatchSize;
      MaxPushQueueSize = settings.MaxLiveQueueSize;

      _liveProcessingStarted = liveProcessingStarted;
      _subscriptionDropped = subscriptionDropped;
      Verbose = settings.VerboseLogging && Log.IsDebugLevelEnabled();
      _subscriptionName = settings.SubscriptionName ?? String.Empty;

      _liveQueue = new BufferBlock<ResolvedEvent>(settings.ToBufferBlockOptions());
    }


    internal Task StartAsync()
    {
      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: starting...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
      return RunSubscriptionAsync();
    }

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

      _liveQueue?.Complete();
      _links?.Dispose();
      foreach (var block in _actionBlocks)
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

    private Task RunSubscriptionAsync() => LoadHistoricalEventsAsync();

    private async Task LoadHistoricalEventsAsync()
    {
      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: running...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

      _stopped.Reset();
      var link = Interlocked.Exchange(ref _links, null);
      link?.Dispose();

      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: pulling events...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        try
        {
          await ReadEventsTillAsync(_connection, _resolveLinkTos, _userCredentials, null, null).ConfigureAwait(false);
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

    private async Task SubscribeToStreamAsync()
    {
      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: subscribing...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        var subscription = StreamId == string.Empty
            ? await _connection.SubscribeToAllAsync(_resolveLinkTos, eventAppearedAsync: EnqueuePushedEventAsync, subscriptionDropped: ServerSubscriptionDropped, userCredentials: _userCredentials).ConfigureAwait(false)
            : await _connection.SubscribeToStreamAsync(_streamId, _resolveLinkTos, eventAppearedAsync: EnqueuePushedEventAsync, subscriptionDropped: ServerSubscriptionDropped, userCredentials: _userCredentials).ConfigureAwait(false);

        _subscription = subscription;
        await ReadMissedHistoricEventsAsync().ConfigureAwait(false);
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }

    private async Task ReadMissedHistoricEventsAsync()
    {
      if (!ShouldStop)
      {
        if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: pulling events (if left)...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

        await ReadEventsTillAsync(_connection, _resolveLinkTos, _userCredentials, _subscription.LastCommitPosition, _subscription.LastEventNumber).ConfigureAwait(false);
        StartLiveProcessing();
      }
      else
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
      }
    }

    private void StartLiveProcessing()
    {
      if (ShouldStop)
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, null);
        return;
      }

      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: processing live events...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);

      _liveProcessingStarted?.Invoke(this);

      if (Verbose) Log.LogDebug("Catch-up Subscription {0} to {1}: hooking to connection.Connected", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
      _connection.Connected += OnReconnect;

      var links = new CompositeDisposable();
      for (var idx = 0; idx < _numActionBlocks; idx++)
      {
        links.Add(_liveQueue.LinkTo(_actionBlocks[idx]));
      }

      Interlocked.Exchange(ref _links, links);
    }

    private async Task EnqueuePushedEventAsync(EventStoreSubscription subscription, ResolvedEvent e)
    {
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: event appeared ({2}, {3}, {4} @ {5}).",
                  SubscriptionName,
                  IsSubscribedToAll ? "<all>" : StreamId,
                  e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEvent.EventType, e.OriginalPosition);
      }

      if (InputCount >= MaxPushQueueSize)
      {
        EnqueueSubscriptionDropNotification(SubscriptionDropReason.ProcessingQueueOverflow, null);
        subscription.Unsubscribe();
        return;
      }

      await _liveQueue.SendAsync(e).ConfigureAwait(false);
    }

    private void ServerSubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exc)
    {
      EnqueueSubscriptionDropNotification(reason, exc);
    }

    private void EnqueueSubscriptionDropNotification(SubscriptionDropReason reason, Exception error)
    {
      // if drop data was already set -- no need to enqueue drop again, somebody did that already
      var dropData = new DropData(reason, error);
      if (Interlocked.CompareExchange(ref _dropData, dropData, null) == null)
      {
        _liveQueue.Post(DropSubscriptionEvent);
      }
    }

    private void ProcessLiveQueue(ResolvedEvent e)
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
        Log.LogDebug("Catch-up Subscription {0} to {1} Exception occurred in subscription {1}", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, exc);
        DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
      }
    }
    private async Task ProcessLiveQueueAsync(ResolvedEvent e)
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
        Log.LogDebug("Catch-up Subscription {0} to {1} Exception occurred in subscription {1}", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, exc);
        DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
      }
    }
    internal void DropSubscription(SubscriptionDropReason reason, Exception error)
    {
      if (Interlocked.CompareExchange(ref _isDropped, 1, 0) == 0)
      {
        if (Verbose)
        {
          Log.LogDebug("Catch-up Subscription {0} to {1}: dropping subscription, reason: {2} {3}.",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId,
                    reason, error == null ? string.Empty : error.ToString());
        }

        _subscription?.Unsubscribe();
        _subscriptionDropped?.Invoke(this, reason, error);
        _stopped.Set();
      }
    }

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
  }

  /// <summary>A catch-up subscription to all events in the Event Store.</summary>
  public class EventStoreAllCatchUpSubscription : EventStoreCatchUpSubscription
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

    internal EventStoreAllCatchUpSubscription(IEventStoreConnection connection,
                                                  Position? fromPositionExclusive, /* if null -- from the very beginning */
                                                  UserCredentials userCredentials,
                                                  Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
                                                  Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                                  Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                  CatchUpSubscriptionSettings settings)
      : base(connection, string.Empty, userCredentials, eventAppeared, liveProcessingStarted, subscriptionDropped, settings)
    {
      _lastProcessedPosition = fromPositionExclusive ?? new Position(-1, -1);
      _nextReadPosition = fromPositionExclusive ?? Position.Start;
    }

    internal EventStoreAllCatchUpSubscription(IEventStoreConnection connection,
                                                  Position? fromPositionExclusive, /* if null -- from the very beginning */
                                                  UserCredentials userCredentials,
                                                  Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
                                                  Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                                  Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                  CatchUpSubscriptionSettings settings)
      : base(connection, string.Empty, userCredentials, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, settings)
    {
      _lastProcessedPosition = fromPositionExclusive ?? new Position(-1, -1);
      _nextReadPosition = fromPositionExclusive ?? Position.Start;
    }

    /// <summary>Read events until the given position async.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events.</param>
    /// <param name="userCredentials">User credentials for the operation.</param>
    /// <param name="lastCommitPosition">The commit position to read until.</param>
    /// <param name="lastEventNumber">The event number to read until.</param>
    /// <returns></returns>
    protected override Task ReadEventsTillAsync(IEventStoreConnection connection, bool resolveLinkTos,
      UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
        => ReadEventsInternalAsync(connection, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber);

    private async Task ReadEventsInternalAsync(IEventStoreConnection connection, bool resolveLinkTos,
      UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      var slice = await connection.ReadAllEventsForwardAsync(_nextReadPosition, ReadBatchSize, resolveLinkTos, userCredentials).ConfigureAwait(false);
      await ReadEventsCallbackAsync(slice, connection, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
    }

    private async Task ReadEventsCallbackAsync(AllEventsSlice slice, IEventStoreConnection connection, bool resolveLinkTos,
                   UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      if (!(await ProcessEventsAsync(lastCommitPosition, slice).ConfigureAwait(false)) && !ShouldStop)
      {
        await ReadEventsInternalAsync(connection, resolveLinkTos, userCredentials,
            lastCommitPosition, lastEventNumber).ConfigureAwait(false);
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

    private async Task<bool> ProcessEventsAsync(long? lastCommitPosition, AllEventsSlice slice)
    {
      if (EventAppeared != null)
      {
        foreach (var e in slice.Events)
        {
          if (e.OriginalPosition == null) throw new Exception($"Subscription {SubscriptionName} event came up with no OriginalPosition.");
          TryProcess(e);
        }
      }
      else
      {
        foreach (var e in slice.Events)
        {
          if (e.OriginalPosition == null) throw new Exception($"Subscription {SubscriptionName} event came up with no OriginalPosition.");
          await TryProcessAsync(e).ConfigureAwait(false);
        }
      }

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

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
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

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
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

  /// <summary>A catch-up subscription to a single stream in the Event Store.</summary>
  public class EventStoreStreamCatchUpSubscription : EventStoreCatchUpSubscription
  {
    /// <summary>The last event number processed on the subscription.</summary>
    public long LastProcessedEventNumber => _lastProcessedEventNumber;

    private long _nextReadEventNumber;
    private long _lastProcessedEventNumber;

    internal EventStoreStreamCatchUpSubscription(IEventStoreConnection connection,
                                                      string streamId,
                                                      long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                                      UserCredentials userCredentials,
                                                      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
                                                      Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                                      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      CatchUpSubscriptionSettings settings)
      : base(connection, streamId, userCredentials, eventAppeared, liveProcessingStarted, subscriptionDropped, settings)
    {
      Ensure.NotNullOrEmpty(streamId, nameof(streamId));

      _lastProcessedEventNumber = fromEventNumberExclusive ?? -1;
      _nextReadEventNumber = fromEventNumberExclusive ?? 0;
    }

    internal EventStoreStreamCatchUpSubscription(IEventStoreConnection connection,
                                                      string streamId,
                                                      long? fromEventNumberExclusive, /* if null -- from the very beginning */
                                                      UserCredentials userCredentials,
                                                      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
                                                      Action<EventStoreCatchUpSubscription> liveProcessingStarted,
                                                      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      CatchUpSubscriptionSettings settings)
      : base(connection, streamId, userCredentials, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, settings)
    {
      Ensure.NotNullOrEmpty(streamId, nameof(streamId));

      _lastProcessedEventNumber = fromEventNumberExclusive ?? -1;
      _nextReadEventNumber = fromEventNumberExclusive ?? 0;
    }

    /// <summary>Read events until the given event number async.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events.</param>
    /// <param name="userCredentials">User credentials for the operation.</param>
    /// <param name="lastCommitPosition">The commit position to read until.</param>
    /// <param name="lastEventNumber">The event number to read until.</param>
    /// <returns></returns>
    protected override Task ReadEventsTillAsync(IEventStoreConnection connection, bool resolveLinkTos,
      UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
        => ReadEventsInternalAsync(connection, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber);

    private async Task ReadEventsInternalAsync(IEventStoreConnection connection, bool resolveLinkTos,
                   UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      var slice = await connection.ReadStreamEventsForwardAsync(StreamId, _nextReadEventNumber, ReadBatchSize, resolveLinkTos, userCredentials).ConfigureAwait(false);
      await ReadEventsCallbackAsync(slice, connection, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
    }

    private async Task ReadEventsCallbackAsync(StreamEventsSlice slice, IEventStoreConnection connection, bool resolveLinkTos,
                   UserCredentials userCredentials, long? lastCommitPosition, long? lastEventNumber)
    {
      if (!(await ProcessEventsAsync(lastEventNumber, slice).ConfigureAwait(false)) && !ShouldStop)
      {
        await ReadEventsInternalAsync(connection, resolveLinkTos, userCredentials, lastCommitPosition, lastEventNumber).ConfigureAwait(false);
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

    private async Task<bool> ProcessEventsAsync(long? lastEventNumber, StreamEventsSlice slice)
    {
      bool done;
      switch (slice.Status)
      {
        case SliceReadStatus.Success:
          {
            if (EventAppeared != null)
            {
              foreach (var e in slice.Events)
              {
                TryProcess(e);
              }
            }
            else
            {
              foreach (var e in slice.Events)
              {
                await TryProcessAsync(e).ConfigureAwait(false);
              }
            }
            _nextReadEventNumber = slice.NextEventNumber;
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

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
    protected override void TryProcess(ResolvedEvent e)
    {
      bool processed = false;
      if (e.OriginalEventNumber > _lastProcessedEventNumber)
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
        _lastProcessedEventNumber = e.OriginalEventNumber;
        processed = true;
      }
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId, processed ? "processed" : "skipping",
                    e.OriginalEvent.EventStreamId, e.OriginalEvent.EventNumber, e.OriginalEvent.EventType, e.OriginalEventNumber);
      }
    }

    /// <summary>Try to process a single <see cref="ResolvedEvent"/>.</summary>
    /// <param name="e">The <see cref="ResolvedEvent"/> to process.</param>
    protected override async Task TryProcessAsync(ResolvedEvent e)
    {
      bool processed = false;
      if (e.OriginalEventNumber > _lastProcessedEventNumber)
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
        _lastProcessedEventNumber = e.OriginalEventNumber;
        processed = true;
      }
      if (Verbose)
      {
        Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                    SubscriptionName,
                    IsSubscribedToAll ? "<all>" : StreamId, processed ? "processed" : "skipping",
                    e.OriginalEvent.EventStreamId, e.OriginalEvent.EventNumber, e.OriginalEvent.EventType, e.OriginalEventNumber);
      }
    }
  }
}
