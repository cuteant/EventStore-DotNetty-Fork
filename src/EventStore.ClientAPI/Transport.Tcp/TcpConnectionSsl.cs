﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.Pool;
using EventStore.ClientAPI.Common.Utils;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Transport.Tcp
{
  internal class TcpConnectionSsl : TcpConnectionBase, ITcpConnection
  {
    public static ITcpConnection CreateConnectingConnection(Guid connectionId,
                                                                IPEndPoint remoteEndPoint,
                                                                string targetHost,
                                                                bool validateServer,
                                                                TcpClientConnector connector,
                                                                TimeSpan connectionTimeout,
                                                                Action<ITcpConnection> onConnectionEstablished,
                                                                Action<ITcpConnection, SocketError> onConnectionFailed,
                                                                Action<ITcpConnection, SocketError> onConnectionClosed)
    {
      var connection = new TcpConnectionSsl(connectionId, remoteEndPoint, onConnectionClosed);
      // ReSharper disable ImplicitlyCapturedClosure
      connector.InitConnect(remoteEndPoint,
                            (_, socket) =>
                            {
                              connection.InitClientSocket(socket, targetHost, validateServer);
                              if (onConnectionEstablished != null)
                                ThreadPool.QueueUserWorkItem(o => onConnectionEstablished(connection));
                            },
                            (_, socketError) =>
                            {
                              if (onConnectionFailed != null)
                                ThreadPool.QueueUserWorkItem(o => onConnectionFailed(connection, socketError));
                            }, connection, connectionTimeout);
      // ReSharper restore ImplicitlyCapturedClosure
      return connection;
    }

    public Guid ConnectionId { get { return _connectionId; } }
    public int SendQueueSize { get { return _sendQueue.Count; } }

    private readonly Guid _connectionId;
    private static readonly ILogger _log = TraceLogger.GetLogger<TcpConnectionSsl>();

    private readonly ConcurrentQueue<ArraySegment<byte>> _sendQueue = new ConcurrentQueue<ArraySegment<byte>>();
    private readonly ConcurrentQueue<ArraySegment<byte>> _receiveQueue = new ConcurrentQueue<ArraySegment<byte>>();
    private readonly MemoryStream _memoryStream = new MemoryStream();

    private readonly object _streamLock = new object();
    private bool _isSending;
    private int _receiveHandling;
    private int _isClosed;

    private Action<ITcpConnection, IEnumerable<ArraySegment<byte>>> _receiveCallback;
    private readonly Action<ITcpConnection, SocketError> _onConnectionClosed;

    private SslStream _sslStream;
    private bool _isAuthenticated;
    private int _sendingBytes;
    private bool _validateServer;
    private readonly byte[] _receiveBuffer = new byte[TcpConfiguration.SocketBufferSize];

    private TcpConnectionSsl(Guid connectionId, IPEndPoint remoteEndPoint,
                               Action<ITcpConnection, SocketError> onConnectionClosed) : base(remoteEndPoint)
    {
      Ensure.NotEmptyGuid(connectionId, "connectionId");

      _connectionId = connectionId;
      _onConnectionClosed = onConnectionClosed;
    }

    private async void InitClientSocket(Socket socket, string targetHost, bool validateServer)
    {
      Ensure.NotNull(targetHost, "targetHost");

      InitConnectionBase(socket);
      //_log.Info("TcpConnectionSsl::InitClientSocket({0}, L{1})", RemoteEndPoint, LocalEndPoint);

      _validateServer = validateServer;


      try
      {
        socket.NoDelay = true;
      }
      catch (ObjectDisposedException)
      {
        CloseInternal(SocketError.Shutdown, "Socket is disposed.");
        return;
      }

      lock (_streamLock) //TODO: this lock needs looking at
      {
        _sslStream = new SslStream(new NetworkStream(socket, true), false, ValidateServerCertificate, null);
      }
      try
      {
        await _sslStream.AuthenticateAsClientAsync(targetHost);
        DisplaySslStreamInfo(_sslStream);
        lock (_streamLock)
        {
          _isAuthenticated = true;
        }
      }
      catch (AuthenticationException exc)
      {
        if (_log.IsInformationLevelEnabled())
        {
          _log.LogInformation(exc, "[S{0}, L{1}]: Authentication exception on BeginAuthenticateAsClient.", RemoteEndPoint, LocalEndPoint);
        }
        CloseInternal(SocketError.SocketError, exc.Message);
      }
      catch (ObjectDisposedException)
      {
        CloseInternal(SocketError.SocketError, "SslStream disposed.");
      }
      catch (Exception exc)
      {
        if (_log.IsInformationLevelEnabled())
        {
          _log.LogInformation(exc, "[S{0}, L{1}]: Exception on BeginAuthenticateAsClient.", RemoteEndPoint, LocalEndPoint);
        }
        CloseInternal(SocketError.SocketError, exc.Message);
      }
      StartReceive();
      TrySend();
    }

    // The following method is invoked by the RemoteCertificateValidationDelegate. 
    public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (!_validateServer) { return true; }

      if (sslPolicyErrors == SslPolicyErrors.None) { return true; }
      _log.LogError("[S{0}, L{1}]: Certificate error: {1}", RemoteEndPoint, LocalEndPoint, sslPolicyErrors);
      // Do not allow this client to communicate with unauthenticated servers. 
      return false;
    }

    private void DisplaySslStreamInfo(SslStream stream)
    {
      if (!_log.IsInformationLevelEnabled()) { return; }

      var sb = StringBuilderManager.Allocate();
      sb.AppendFormat("[S{0}, L{1}]:\n", RemoteEndPoint, LocalEndPoint);
      sb.AppendFormat("Cipher: {0} strength {1}\n", stream.CipherAlgorithm, stream.CipherStrength);
      sb.AppendFormat("Hash: {0} strength {1}\n", stream.HashAlgorithm, stream.HashStrength);
      sb.AppendFormat("Key exchange: {0} strength {1}\n", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
      sb.AppendFormat("Protocol: {0}\n", stream.SslProtocol);
      sb.AppendFormat("Is authenticated: {0} as server? {1}\n", stream.IsAuthenticated, stream.IsServer);
      sb.AppendFormat("IsSigned: {0}\n", stream.IsSigned);
      sb.AppendFormat("Is Encrypted: {0}\n", stream.IsEncrypted);
      sb.AppendFormat("Can read: {0}, write {1}\n", stream.CanRead, stream.CanWrite);
      sb.AppendFormat("Can timeout: {0}\n", stream.CanTimeout);
      sb.AppendFormat("Certificate revocation list checked: {0}\n", stream.CheckCertRevocationStatus);

      if (stream.LocalCertificate is X509Certificate2 localCert)
      {
        sb.AppendFormat("Local certificate was issued to {0} and is valid from {1} until {2}.\n",
                        localCert.Subject, localCert.NotBefore, localCert.NotAfter);
      }
      else
      {
        sb.AppendFormat("Local certificate is null.\n");
      }

      // Display the properties of the client's certificate.
      if (stream.RemoteCertificate is X509Certificate2 remoteCert)
      {
        sb.AppendFormat("Remote certificate was issued to {0} and is valid from {1} until {2}.\n",
                        remoteCert.Subject, remoteCert.NotBefore, remoteCert.NotAfter);
      }
      else
      {
        sb.AppendFormat("Remote certificate is null.\n");
      }

      _log.LogInformation(StringBuilderManager.ReturnAndFree(sb));
    }

    public void EnqueueSend(IEnumerable<ArraySegment<byte>> data)
    {
      lock (_streamLock)
      {
        int bytes = 0;
        foreach (var segment in data)
        {
          _sendQueue.Enqueue(segment);
          bytes += segment.Count;
        }
        NotifySendScheduled(bytes);
      }
      TrySend();
    }

    private async void TrySend()
    {
      do
      {
        lock (_streamLock)
        {
          if (_isSending || _sendQueue.Count == 0 || _sslStream == null || !_isAuthenticated) return;
          if (TcpConnectionMonitor.Default.IsSendBlocked()) return;
          _isSending = true;
        }

        _memoryStream.SetLength(0);

        while (_sendQueue.TryDequeue(out ArraySegment<byte> sendPiece))
        {
          _memoryStream.Write(sendPiece.Array, sendPiece.Offset, sendPiece.Count);
          if (_memoryStream.Length >= TcpConnection.MaxSendPacketSize)
            break;
        }
        _sendingBytes = (int)_memoryStream.Length;

        try
        {
          NotifySendStarting(_sendingBytes);
#if NET_4_5_GREATER
          _memoryStream.TryGetBuffer(out ArraySegment<byte> buffer);
          await _sslStream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count);
#else
          await _sslStream.WriteAsync(_memoryStream.GetBuffer(), 0, (int)_memoryStream.Length);
#endif
          NotifySendCompleted(_sendingBytes);

          lock (_streamLock)
          {
            _isSending = false;
          }
        }
        catch (SocketException exc)
        {
          if (_log.IsDebugLevelEnabled()) _log.LogDebug(exc, "SocketException '{0}' during BeginWrite.", exc.SocketErrorCode);
          CloseInternal(exc.SocketErrorCode, "SocketException during BeginWrite.");
        }
        catch (ObjectDisposedException)
        {
          CloseInternal(SocketError.SocketError, "SslStream disposed.");
          return;
        }
        catch (Exception exc)
        {
          if (_log.IsDebugLevelEnabled()) _log.LogDebug(exc, "Exception during BeginWrite.");
          CloseInternal(SocketError.SocketError, "Exception during BeginWrite");
          return;
        }
      } while (true);
    }


    public void ReceiveAsync(Action<ITcpConnection, IEnumerable<ArraySegment<byte>>> callback)
    {
      Ensure.NotNull(callback, "callback");

      if (Interlocked.Exchange(ref _receiveCallback, callback) != null)
      {
        _log.LogError("ReceiveAsync called again while previous call was not fulfilled");
        throw new InvalidOperationException("ReceiveAsync called again while previous call was not fulfilled");
      }
      TryDequeueReceivedData();
    }

    private async void StartReceive()
    {
      try
      {
        while (true)
        {
          NotifyReceiveStarting();
          var bytesRead = await _sslStream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length);

          if (bytesRead <= 0) // socket closed normally
          {
            NotifyReceiveCompleted(0);
            CloseInternal(SocketError.Success, "Socket closed.");
            return;
          }

          NotifyReceiveCompleted(bytesRead);

          var buffer = new ArraySegment<byte>(new byte[bytesRead]);
          Buffer.BlockCopy(_receiveBuffer, 0, buffer.Array, buffer.Offset, bytesRead);
          _receiveQueue.Enqueue(buffer);


          TryDequeueReceivedData();
        }
      }
      catch (SocketException exc)
      {
        if (_log.IsDebugLevelEnabled()) _log.LogDebug(exc, "SocketException '{0}' during BeginRead.", exc.SocketErrorCode);
        CloseInternal(exc.SocketErrorCode, "SocketException during BeginRead.");
      }
      catch (ObjectDisposedException)
      {
        CloseInternal(SocketError.SocketError, "SslStream disposed.");
      }
      catch (Exception exc)
      {
        if (_log.IsDebugLevelEnabled()) _log.LogDebug(exc, "Exception during BeginRead.");
        CloseInternal(SocketError.SocketError, "Exception during BeginRead.");
      }
    }

    private async void TryDequeueReceivedData()
    {
      if (Interlocked.CompareExchange(ref _receiveHandling, 1, 0) != 0) { return; }

      await Task.Yield();
      do
      {
        if (_receiveQueue.Count > 0 && _receiveCallback != null)
        {
          var callback = Interlocked.Exchange(ref _receiveCallback, null);
          if (callback == null)
          {
            _log.LogError("Threading issue in TryDequeueReceivedData. Callback is null.");
            throw new Exception("Threading issue in TryDequeueReceivedData. Callback is null.");
          }

          var res = new List<ArraySegment<byte>>(_receiveQueue.Count);
          while (_receiveQueue.TryDequeue(out ArraySegment<byte> piece))
          {
            res.Add(piece);
          }

          callback(this, res);

          int bytes = 0;
          for (int i = 0, n = res.Count; i < n; ++i)
          {
            bytes += res[i].Count;
          }
          NotifyReceiveDispatched(bytes);
        }
        Interlocked.Exchange(ref _receiveHandling, 0);
      } while (_receiveQueue.Count > 0
               && _receiveCallback != null
               && Interlocked.CompareExchange(ref _receiveHandling, 1, 0) == 0);
    }

    public void Close(string reason)
    {
      CloseInternal(SocketError.Success, reason ?? "Normal socket close."); // normal socket closing
    }

    private void CloseInternal(SocketError socketError, string reason)
    {
      if (Interlocked.CompareExchange(ref _isClosed, 1, 0) != 0) { return; }

      NotifyClosed();

      if (_log.IsInformationLevelEnabled())
      {
        _log.LogInformation("ClientAPI {0} closed [{1:HH:mm:ss.fff}: S{2}, L{3}, {4:B}]:", GetType().Name, DateTime.UtcNow, RemoteEndPoint, LocalEndPoint, _connectionId);
        _log.LogInformation("Received bytes: {0}, Sent bytes: {1}", TotalBytesReceived, TotalBytesSent);
        _log.LogInformation("Send calls: {0}, callbacks: {1}", SendCalls, SendCallbacks);
        _log.LogInformation("Receive calls: {0}, callbacks: {1}", ReceiveCalls, ReceiveCallbacks);
        _log.LogInformation("Close reason: [{0}] {1}", socketError, reason);
      }

      if (_sslStream != null)
      {
        Helper.EatException(() => _sslStream.Dispose());
      }

      _onConnectionClosed?.Invoke(this, socketError);
    }

    public override string ToString() => "S" + RemoteEndPoint;
  }
}
