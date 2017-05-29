﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace Es.Receiver
{
  class Program
  {
    const string STREAM = "a_test_stream";
    const string GROUP = "a_test_group";
    const int DEFAULTPORT = 1113;

    static void Main(string[] args)
    {
      var logFactory = new LoggerFactory();
      logFactory.AddNLog();
      TraceLogger.Initialize(logFactory);

      var connStr = "ConnectTo=tcp://admin:changeit@localhost:1113";
      var connSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying();
      using (var conn = EventStoreConnection.Create(connStr, connSettings))
      {
        conn.ConnectAsync().Wait();

        var eventSlice = conn.ReadStreamEventsForwardAsync(STREAM, 0, 2000, true).GetAwaiter().GetResult();

        Console.WriteLine("FromEventNumber: {0}", eventSlice.FromEventNumber);
        Console.WriteLine("NextEventNumber: {0}", eventSlice.NextEventNumber);
        Console.WriteLine("LastEventNumber: {0}", eventSlice.LastEventNumber);
        Console.WriteLine("IsEndOfStream: {0}", eventSlice.IsEndOfStream);

        foreach (var x in eventSlice.Events)
        {
          var data = Encoding.ASCII.GetString(x.OriginalEvent.Data);
          Console.WriteLine("Received: " + x.OriginalEvent.EventStreamId + ":" + x.OriginalEvent.EventNumber);
          Console.WriteLine(data);
        }




        //var readEvent = conn.ReadEventAsync(STREAM, 1, true).GetAwaiter().GetResult(); ;
        //Console.WriteLine("Stream: {0}", readEvent.Stream);
        //Console.WriteLine("Status: {0}", readEvent.Status);
        //Console.WriteLine("EventNumber: {0}", readEvent.EventNumber);
        //if (readEvent.Status == EventReadStatus.Success)
        //{
        //  Console.WriteLine("Received: " + Encoding.ASCII.GetString(readEvent.Event.Value.OriginalEvent.Data));
        //}
        //Console.WriteLine("");

        //readEvent = conn.ReadEventAsync(STREAM, 1000000, true).GetAwaiter().GetResult(); ;
        //Console.WriteLine("Stream: {0}", readEvent.Stream);
        //Console.WriteLine("Status: {0}", readEvent.Status);
        //Console.WriteLine("EventNumber: {0}", readEvent.EventNumber);
        //if (readEvent.Status == EventReadStatus.Success)
        //{
        //  Console.WriteLine("Received: " + Encoding.ASCII.GetString(readEvent.Event.Value.OriginalEvent.Data));
        //}
        //Console.WriteLine("");

        //readEvent = conn.ReadFirstEventAsync(STREAM, true).GetAwaiter().GetResult(); ;
        //Console.WriteLine("Stream: {0}", readEvent.Stream);
        //Console.WriteLine("Status: {0}", readEvent.Status);
        //Console.WriteLine("EventNumber: {0}", readEvent.EventNumber);
        //if (readEvent.Status == EventReadStatus.Success)
        //{
        //  Console.WriteLine("Received: " + Encoding.ASCII.GetString(readEvent.Event.Value.OriginalEvent.Data));
        //}
        //Console.WriteLine("");

        //readEvent = conn.ReadLastEventAsync(STREAM, true).GetAwaiter().GetResult(); ;
        //Console.WriteLine("Stream: {0}", readEvent.Stream);
        //Console.WriteLine("Status: {0}", readEvent.Status);
        //Console.WriteLine("EventNumber: {0}", readEvent.EventNumber);
        //if (readEvent.Status == EventReadStatus.Success)
        //{
        //  Console.WriteLine("Received: " + Encoding.ASCII.GetString(readEvent.Event.Value.OriginalEvent.Data));
        //}
        //Console.WriteLine("");

        //readEvent = conn.ReadLastEventAsync(STREAM + "null", true).GetAwaiter().GetResult(); ;
        //Console.WriteLine("Stream: {0}", readEvent.Stream);
        //Console.WriteLine("Status: {0}", readEvent.Status);
        //Console.WriteLine("EventNumber: {0}", readEvent.EventNumber);
        //if (readEvent.Status == EventReadStatus.Success)
        //{
        //  Console.WriteLine("Received: " + Encoding.ASCII.GetString(readEvent.Event.Value.OriginalEvent.Data));
        //}

        Console.WriteLine("waiting for events. press enter to exit");
        Console.ReadKey();
      }

    }
  }
}
