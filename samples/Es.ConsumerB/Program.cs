﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace Es.Consumer
{
  class Program
  {
    const string STREAM = "a_test_stream";
    const string GROUP = "a_test_group_2";
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

        //Normally the creating of the subscription group is not done in your general executable code. 
        //Instead it is normally done as a step during an install or as an admin task when setting 
        //things up. You should assume the subscription exists in your code.
        //CreateSubscription(conn);
        UpdateSubscription(conn);

        conn.ConnectToPersistentSubscription(STREAM, GROUP, async (_, x) =>
        {
          await TaskConstants.Completed;
          var data = Encoding.ASCII.GetString(x.Event.Data);
          if (x.Event.EventNumber % 3 == 0)
          {
            //var errorMsg = $"error event number: {x.Event.EventNumber}";
            //Console.WriteLine(errorMsg);
            //throw new InvalidOperationException(errorMsg);
          }
          Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
          Console.WriteLine(data);
        },
        (subscription, reason, exc) =>
        {
          Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        },
        null, 10, true);

        //conn.ConnectToPersistentSubscription(STREAM, GROUP, async (_, x) =>
        //{
        //  await TaskConstants.Completed;
        //  var data = Encoding.ASCII.GetString(x.Event.Data);
        //  Console.WriteLine("2 Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //  Console.WriteLine(data);
        //}, null, null, 10, true);

        #region VolatileSubscription
        //var sub = conn.SubscribeToStreamAsync(STREAM, true,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      await TaskConstants.Completed;
        //      var data = Encoding.ASCII.GetString(x.Event.Data);
        //      if (x.Event.EventNumber % 3 == 0)
        //      {
        //        var errorMsg = $"error event number: {x.Event.EventNumber}";
        //        Console.WriteLine(errorMsg);
        //        throw new InvalidOperationException(errorMsg);
        //      }
        //      Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //      Console.WriteLine(data);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        //var sub1 = conn.SubscribeToStreamAsync(STREAM, true,
        //    async (_, x) =>
        //    {
        //      await TaskConstants.Completed;
        //      var data = Encoding.ASCII.GetString(x.Event.Data);
        //      Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //      Console.WriteLine(data);
        //    });
        #endregion

        #region CatchupSubscription
        //Note the subscription is subscribing from the beginning every time. You could also save
        //your checkpoint of the last seen event and subscribe to that checkpoint at the beginning.
        //If stored atomically with the processing of the event this will also provide simulated
        //transactional messaging.

        //var settings = CatchUpSubscriptionSettings.Create(true);

        //var sub = conn.SubscribeToStreamFrom(STREAM, StreamPosition.Start, settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      await TaskConstants.Completed;
        //      var data = Encoding.ASCII.GetString(x.Event.Data);
        //      if (x.Event.EventNumber % 3 == 0)
        //      {
        //        var errorMsg = $"error event number: {x.Event.EventNumber}";
        //        Console.WriteLine(errorMsg);
        //        throw new InvalidOperationException(errorMsg);
        //      }
        //      Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //      Console.WriteLine(data);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        //var sub1 = conn.SubscribeToStreamFrom(STREAM, StreamPosition.Start, settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      await TaskConstants.Completed;
        //      var data = Encoding.ASCII.GetString(x.Event.Data);
        //      Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //      Console.WriteLine(data);
        //    });
        #endregion

        Console.WriteLine("waiting for events. press enter to exit");
        Console.ReadKey();
      }
    }

    private static void CreateSubscription(IEventStoreConnection conn)
    {
      PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
          .DoNotResolveLinkTos()
          .StartFromCurrent()
          .PreferRoundRobin();

      conn.CreatePersistentSubscription(STREAM, GROUP, settings);
    }

    private static void UpdateSubscription(IEventStoreConnection conn)
    {
      PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
          .DoNotResolveLinkTos()
          .StartFromCurrent();

      conn.UpdatePersistentSubscription(STREAM, GROUP, settings);
    }
  }
}
