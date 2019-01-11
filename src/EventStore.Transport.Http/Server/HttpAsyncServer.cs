using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Http.Server
{
    public sealed class HttpAsyncServer
    {
        private static readonly ILogger Logger = TraceLogger.GetLogger<HttpAsyncServer>();

        public event Action<HttpAsyncServer, HttpListenerContext> RequestReceived;

        public bool IsListening { get { return _listener.IsListening; } }
        public readonly string[] _listenPrefixes;

        private HttpListener _listener;


        public HttpAsyncServer(string[] prefixes)
        {
            if (null == prefixes) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.prefixes); }

            _listenPrefixes = prefixes;

            CreateListener(prefixes);
        }

        private void CreateListener(IEnumerable<string> prefixes)
        {
            _listener = new HttpListener
            {
                Realm = "ES",
                AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous
            };
            foreach (var prefix in prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
        }

        public bool TryStart()
        {
            try
            {
                if (Logger.IsInformationLevelEnabled()) Logger.LogInformation("Starting HTTP server on [{0}]...", string.Join(",", _listener.Prefixes));
                try
                {
                    _listener.Start();
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode == 5) //Access error don't see any better way of getting it
                    {
                        if (_listenPrefixes.Length > 0)
                            TryAddAcl(_listenPrefixes[0]);
                        CreateListener(_listenPrefixes);
                        if (Logger.IsInformationLevelEnabled()) Logger.LogInformation("Retrying HTTP server on [{0}]...", string.Join(",", _listener.Prefixes));
                        _listener.Start();
                    }
                }

                _listener.BeginGetContext(ContextAcquired, null);

                if (Logger.IsInformationLevelEnabled()) Logger.LogInformation("HTTP server is up and listening on [{0}]", string.Join(",", _listener.Prefixes));

                return true;
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, "Failed to start http server");
                return false;
            }
        }

        private void TryAddAcl(string address)
        {
#if DESKTOPCLR
            var args = string.Format("http add urlacl url={0} user=\"{1}\\{2}\"", address, Environment.UserDomainName, Environment.UserName);
            if (Logger.IsInformationLevelEnabled()) Logger.LogInformation("Attempting to add permissions for " + address + " using netsh " + args);
            var startInfo = new ProcessStartInfo("netsh", args)
            {
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };

            var aclProcess = Process.Start(startInfo);

            if (aclProcess != null)
                aclProcess.WaitForExit();
#endif
        }

        public void Shutdown()
        {
            try
            {
                var counter = 10;
                while (_listener.IsListening && counter-- > 0)
                {
                    _listener.Abort();
                    _listener.Stop();
                    _listener.Close();
                    if (_listener.IsListening)
                        System.Threading.Thread.Sleep(50);
                }
            }
            catch (ObjectDisposedException)
            {
                // that's ok
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while shutting down http server");
            }
        }

        private void ContextAcquired(IAsyncResult ar)
        {
            if (!IsListening)
                return;

            HttpListenerContext context = null;
            bool success = false;
            try
            {
                context = _listener.EndGetContext(ar);
                success = true;
            }
            catch (HttpListenerException)
            {
                // that's not application-level error, ignore and continue
            }
            catch (ObjectDisposedException)
            {
                // that's ok, just continue
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception e)
            {
                if (Logger.IsDebugLevelEnabled()) Logger.LogDebug(e, "EndGetContext exception. Status : {0}.", IsListening ? "listening" : "stopped");
            }

            if (success)
                try
                {
                    ProcessRequest(context);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (InvalidOperationException)
                {
                }
                catch (ApplicationException)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "ProcessRequest error");
                }

            try
            {
                _listener.BeginGetContext(ContextAcquired, null);
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (ApplicationException)
            {
            }
            catch (Exception e)
            {
                Logger.LogError(e, "BeginGetContext error. Status : {0}.", IsListening ? "listening" : "stopped");
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            context.Response.StatusCode = HttpStatusCode.InternalServerError;
            OnRequestReceived(context);
        }

        private void OnRequestReceived(HttpListenerContext context)
        {
            var handler = RequestReceived;
            if (handler != null)
                handler(this, context);
        }
    }
}