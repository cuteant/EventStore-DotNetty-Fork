﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Rx
{
  public interface IConnectionStatusMonitor : IDisposable
  {
    IObservable<ConnectionInfo> ConnectionInfoChanged { get; }
  }

  public class ConnectionStatusMonitor : IConnectionStatusMonitor
  {
    private static readonly ILogger s_logger = TraceLogger.GetLogger<ConnectionStatusMonitor>();

    private readonly IConnectableObservable<ConnectionInfo> _connectionInfoChanged;
    private readonly IDisposable _connection;

    public ConnectionStatusMonitor(IEventStoreConnection connection)
    {
      var connectedChanged = Observable.FromEventPattern<ClientConnectionEventArgs>(h => connection.Connected += h,
                                                                                    h => connection.Connected -= h)
                                       .Select(_ => ConnectionStatus.Connected);

      var disconnectedChanged = Observable.FromEventPattern<ClientConnectionEventArgs>(h => connection.Disconnected += h,
                                                                                       h => connection.Disconnected -= h)
                                          .Select(_ => ConnectionStatus.Disconnected);

      var reconnectingChanged = Observable.FromEventPattern<ClientReconnectingEventArgs>(h => connection.Reconnecting += h,
                                                                                         h => connection.Reconnecting -= h)
                                          .Select(_ => ConnectionStatus.Connecting);

      _connectionInfoChanged = Observable.Merge(connectedChanged, disconnectedChanged, reconnectingChanged)
                                         .Scan(ConnectionInfo.Initial, UpdateConnectionInfo)
                                         .StartWith(ConnectionInfo.Initial)
                                         .Replay(1);

      _connection = _connectionInfoChanged.Connect();
    }

    public IObservable<ConnectionInfo> ConnectionInfoChanged => _connectionInfoChanged;

    public void Dispose()
    {
      _connection.Dispose();
    }

    private static ConnectionInfo UpdateConnectionInfo(ConnectionInfo previousConnectionInfo, ConnectionStatus connectionStatus)
    {
      ConnectionInfo newConnectionInfo;

      if ((previousConnectionInfo.Status == ConnectionStatus.Disconnected ||
           previousConnectionInfo.Status == ConnectionStatus.Connecting) &&
          connectionStatus == ConnectionStatus.Connected)
      {
        newConnectionInfo = new ConnectionInfo(connectionStatus, previousConnectionInfo.ConnectCount + 1);
      }
      else
      {
        newConnectionInfo = new ConnectionInfo(connectionStatus, previousConnectionInfo.ConnectCount);
      }

      if (s_logger.IsInformationLevelEnabled())
      {
        s_logger.LogInformation(newConnectionInfo.ToString());
      }

      return newConnectionInfo;
    }
  }
}