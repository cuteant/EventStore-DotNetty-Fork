using System;
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
using Es.SharedModels;

namespace Es.Consumer
{
  class Program
  {
    const string STREAM = "test-animal";
    const string GROUP = "a_test_group_1";

    static void Main(string[] args)
    {
      var logFactory = new LoggerFactory();
      logFactory.AddNLog();
      TraceLogger.Initialize(logFactory);


      var connStr = "ConnectTo=tcp://admin:changeit@localhost:1113";
      var connSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying();//.EnableVerboseLogging();
      using (var conn = EventStoreConnection.Create(connStr, connSettings))
      {
        conn.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        UpdateSubscription(conn);

        //UpdateStreamMetadata(conn);

        #region PersistentSubscription

        var settings = new ConnectToPersistentSubscriptionSettings();
        //var settings = new ConnectToPersistentSubscriptionSettings { MaxDegreeOfParallelismPerBlock = 5 };
        //var settings = new ConnectToPersistentSubscriptionSettings { BoundedCapacityPerBlock = 2, NumActionBlocks = 5 };

        var sub = conn.PersistentSubscribeAsync(STREAM, GROUP,settings,
            addHandlers: _ =>
            _.Add<Cat>(iEvent =>
              {
                var cat = iEvent.OriginalEvent.FullEvent.Value;
                Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
              }
            ).Add<Dog>(iEvent =>
            {
              var dog = iEvent.OriginalEvent.FullEvent.Value;
              Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
            ),
            subscriptionDropped: (subscription, reason, exc) =>
            {
              Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
            });
        //conn.PersistentSubscribeAsync(STREAM, GROUP, settings, async (_, x) =>
        //{
        //  var msg = x.OriginalEvent.FullEvent.Value;
        //  if (msg is Cat cat)
        //  {
        //    Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //  }
        //  if (msg is Dog dog)
        //  {
        //    Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //  }
        //  await Task.Delay(500);
        //},
        //(subscription, reason, exc) =>
        //{
        //  Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //});
        //conn.PersistentSubscribeAsync<IAnimal>(GROUP, settings, async (_, x) =>
        //{
        //  var msg = x.OriginalEvent.FullEvent.Value;
        //  if (msg is Cat cat)
        //  {
        //    Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //  }
        //  if (msg is Dog dog)
        //  {
        //    Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //  }
        //  await Task.Delay(500);
        //},
        //(subscription, reason, exc) =>
        //{
        //  Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //});

        #endregion

        #region VolatileSubscription

        //var settings = new SubscriptionSettings();
        ////var settings = new SubscriptionSettings { MaxDegreeOfParallelismPerBlock = 5 };
        ////var settings = new SubscriptionSettings { BoundedCapacityPerBlock = 2, NumActionBlocks = 5 };

        //var sub = conn.VolatileSubscribeAsync(STREAM, settings,
        //    addHandlers: _ => 
        //    _.Add<Cat>(iEvent =>
        //      {
        //        var cat = iEvent.OriginalEvent.FullEvent.Value;
        //        Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //      }
        //    ).Add<Dog>(iEvent =>
        //    {
        //      var dog = iEvent.OriginalEvent.FullEvent.Value;
        //      Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //    }
        //    ),
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        //var sub = conn.VolatileSubscribeAsync(STREAM, settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      var msg = x.OriginalEvent.FullEvent.Value;
        //      if (msg is Cat cat)
        //      {
        //        Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //      }
        //      if (msg is Dog dog)
        //      {
        //        Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //      }
        //      await Task.Delay(500);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        //var sub = conn.VolatileSubscribeAsync<IAnimal>(settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      var msg = x.OriginalEvent.FullEvent.Value;
        //      if (msg is Cat cat)
        //      {
        //        Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //      }
        //      if (msg is Dog dog)
        //      {
        //        Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //      }
        //      await Task.Delay(500);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        #endregion

        #region CatchupSubscription

        //Note the subscription is subscribing from the beginning every time. You could also save
        //your checkpoint of the last seen event and subscribe to that checkpoint at the beginning.
        //If stored atomically with the processing of the event this will also provide simulated
        //transactional messaging.

        //var settings = CatchUpSubscriptionSettings.Create(20, true);
        ////settings.VerboseLogging = true;

        ////settings.MaxDegreeOfParallelismPerBlock = 5;
        ////settings.BoundedCapacityPerBlock = 2;
        ////settings.NumActionBlocks = 5;

        //var sub = conn.CatchUpSubscribe(STREAM, 1000, settings,
        //    addHandlers: _ =>
        //    _.Add<Cat>(iEvent =>
        //      {
        //        var cat = iEvent.OriginalEvent.FullEvent.Value;
        //        Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //      }
        //    ).Add<Dog>(iEvent =>
        //    {
        //      var dog = iEvent.OriginalEvent.FullEvent.Value;
        //      Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //    }
        //    ),
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        //var sub = conn.CatchUpSubscribe(STREAM, 1000, settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      var msg = x.OriginalEvent.FullEvent.Value;
        //      if (msg is Cat cat)
        //      {
        //        Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //      }
        //      if (msg is Dog dog)
        //      {
        //        Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //      }
        //      await Task.Delay(500);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        //var sub = conn.CatchUpSubscribe<IAnimal>(90, settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      var msg = x.OriginalEvent.FullEvent.Value;
        //      if (msg is Cat cat)
        //      {
        //        Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        //      }
        //      if (msg is Dog dog)
        //      {
        //        Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        //      }
        //      await Task.Delay(500);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        #endregion

        Console.WriteLine("waiting for events. press enter to exit");
        Console.ReadKey();
      }
    }

    private static void UpdateSubscription(IEventStoreConnection conn)
    {
      PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
          .DoNotResolveLinkTos()
          .StartFromBeginning();

      conn.UpdatePersistentSubscription(STREAM, GROUP, settings);
    }

    //private static void UpdateStreamMetadata(IEventStoreConnection conn)
    //{
    //  conn.SetStreamMetadataAsync(STREAM, ExpectedVersion.Any, StreamMetadata.Create(1000, TimeSpan.FromMinutes(30)))
    //      .ConfigureAwait(false).GetAwaiter().GetResult();
    //}
  }
}
