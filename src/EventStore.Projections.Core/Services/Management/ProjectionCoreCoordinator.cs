﻿using System;
using EventStore.Common.Options;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.TimerService;
using EventStore.Projections.Core.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.Projections.Core.Services.Management
{
  public class ProjectionCoreCoordinator
      : IHandle<ProjectionManagementMessage.Internal.RegularTimeout>,
      IHandle<SystemMessage.StateChangeMessage>,
      IHandle<SystemMessage.SystemCoreReady>
  {
    private readonly ILogger Log = TraceLogger.GetLogger<ProjectionCoreCoordinator>();
    private readonly ProjectionType _runProjections;
    private readonly TimeoutScheduler[] _timeoutSchedulers;
    private readonly IPublisher[] _queues;
    private bool _started;
    private readonly IPublisher _publisher;
    private readonly IEnvelope _publishEnvelope;

    public ProjectionCoreCoordinator(
        ProjectionType runProjections,
        TimeoutScheduler[] timeoutSchedulers,
        IPublisher[] queues,

        IPublisher publisher,
        IEnvelope publishEnvelope)
    {
      _runProjections = runProjections;
      _timeoutSchedulers = timeoutSchedulers;
      _queues = queues;
      _publisher = publisher;
      _publishEnvelope = publishEnvelope;
    }

    public void Handle(ProjectionManagementMessage.Internal.RegularTimeout message)
    {
      ScheduleRegularTimeout();
      for (var i = 0; i < _timeoutSchedulers.Length; i++)
        _timeoutSchedulers[i].Tick();
    }

    private bool _systemReady = false;
    private VNodeState _currentState = VNodeState.Unknown;
    private Guid _epochId = Guid.Empty;
    public void Handle(SystemMessage.SystemCoreReady message)
    {
      _systemReady = true;
      StartWhenConditionsAreMet();
    }

    public void Handle(SystemMessage.StateChangeMessage message)
    {
      _currentState = message.State;
      _epochId = GetEpochIdFromStateChange(message);
      StartWhenConditionsAreMet();
    }

    private Guid GetEpochIdFromStateChange(SystemMessage.StateChangeMessage message)
    {
      Guid epochId = Guid.Empty;
      switch (message.State)
      {
        case VNodeState.Master:
          epochId = ((SystemMessage.BecomeMaster)message).EpochId;
          break;
        case VNodeState.Slave:
          epochId = ((SystemMessage.BecomeSlave)message).EpochId;
          break;
      }
      return epochId;
    }

    private void StartWhenConditionsAreMet()
    {
      if (_systemReady && (_currentState == VNodeState.Master || _currentState == VNodeState.Slave))
      {
        if (!_started)
        {
          if (Log.IsDebugLevelEnabled()) Log.LogDebug("PROJECTIONS: Starting Projections Core Coordinator. (Node State : {0})", _currentState);
          Start();
        }
      }
      else
      {
        if (_started)
        {
          if (Log.IsDebugLevelEnabled()) Log.LogDebug("PROJECTIONS: Stopping Projections Core Coordinator. (Node State : {0})", _currentState);
          Stop();
        }
      }
    }

    private void ScheduleRegularTimeout()
    {
      if (!_started)
        return;
      _publisher.Publish(
          TimerMessage.Schedule.Create(
              TimeSpan.FromMilliseconds(100),
              _publishEnvelope,
              new ProjectionManagementMessage.Internal.RegularTimeout()));
    }

    private void Start()
    {
      if (_started) throw new InvalidOperationException();
      _started = true;
      ScheduleRegularTimeout();
      foreach (var queue in _queues)
      {
        queue.Publish(new ReaderCoreServiceMessage.StartReader());
        if (_runProjections >= ProjectionType.System)
        {
          queue.Publish(new ProjectionCoreServiceMessage.StartCore(_epochId));
        }
        else
        {
          _publisher.Publish(new SystemMessage.SubSystemInitialized("Projections"));
        }
      }
    }

    private void Stop()
    {
      if (_started)
      {
        _started = false;
        foreach (var queue in _queues)
        {
          queue.Publish(new ProjectionCoreServiceMessage.StopCore());
          if (_runProjections >= ProjectionType.System)
          {
            queue.Publish(new ReaderCoreServiceMessage.StopReader());
          }
        }
      }
    }

    public void SetupMessaging(IBus bus)
    {
      bus.Subscribe<SystemMessage.StateChangeMessage>(this);
      bus.Subscribe<SystemMessage.SystemCoreReady>(this);
      if (_runProjections >= ProjectionType.System)
      {
        bus.Subscribe<ProjectionManagementMessage.Internal.RegularTimeout>(this);
      }
    }
  }
}