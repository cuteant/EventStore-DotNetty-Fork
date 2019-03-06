using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Consumers;
using EventStore.ClientAPI.Subscriptions;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;
using Es.SharedModels;
using NLog.Extensions.Logging;

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

            var throwEx = false;

            var connStr = "ConnectTo=tcp://admin:changeit@localhost:1113";
            var connSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying();//.EnableVerboseLogging();
            using (var conn = EventStoreConnection.Create(connStr, connSettings))
            {
                conn.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                UpdateSubscription(conn);

                //UpdateStreamMetadata(conn);

                #region PersistentSubscription

                //var settings = new ConnectToPersistentSubscriptionSettings();

                //var subscription = new PersistentSubscription(STREAM, GROUP);
                //subscription.Settings = settings;
                //subscription.PersistentSettings = PersistentSubscriptionSettings.Create().DoNotResolveLinkTos().StartFromBeginning();
                //var persistentConsumer = new PersistentConsumer();
                //persistentConsumer.Initialize(conn, subscription, addEventHandlers: _ =>
                //   _.Add<Cat>(iEvent =>
                //   {
                //       if (iEvent.OriginalEventNumber == 2 && !throwEx)
                //       {
                //           throwEx = true;
                //           throw new Exception();
                //       }
                //       var cat = iEvent.OriginalEvent.FullEvent.Value;
                //       Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //   }
                //   ).Add<Dog>(iEvent =>
                //   {
                //       if (iEvent.OriginalEventNumber == 9)
                //       {
                //           conn.Close();
                //           //persistentConsumer.Dispose();
                //       }
                //       var dog = iEvent.OriginalEvent.FullEvent.Value;
                //       Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //   }
                //   ));
                //persistentConsumer.ConnectToSubscriptionAsync();

                //var sub = conn.PersistentSubscribeAsync(STREAM, GROUP, settings,
                //    addEventHandlers: _ =>
                //    _.Add<Cat>(iEvent =>
                //      {
                //          var cat = iEvent.OriginalEvent.FullEvent.Value;
                //          Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //      }
                //    ).Add<Dog>(iEvent =>
                //    {
                //        var dog = iEvent.OriginalEvent.FullEvent.Value;
                //        Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    ),
                //    subscriptionDropped: (subscription, reason, exc) =>
                //    {
                //        Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
                //    });

                //var sub1 = conn.PersistentSubscribeAsync(STREAM, GROUP, settings,
                //    addHandlers: _ =>
                //    _.Add<Cat>(cat =>
                //    {
                //        Console.WriteLine("Received Cat: " + cat.Name + ":" + cat.Meow);
                //    }
                //    ).Add<Dog>(dog =>
                //    {
                //        Console.WriteLine("Received Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    ),
                //    subscriptionDropped: (subscription, reason, exc) =>
                //    {
                //        Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
                //    });

                //conn.PersistentSubscribeAsync(STREAM, GROUP, settings, async (_, x, c) =>
                //{
                //    var msg = x.OriginalEvent.FullEvent.Value;
                //    if (msg is Cat cat)
                //    {
                //        Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //    }
                //    if (msg is Dog dog)
                //    {
                //        Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    await Task.Delay(500);
                //},
                //(subscription, reason, exc) =>
                //{
                //    Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
                //});

                //conn.PersistentSubscribeAsync<IAnimal>(GROUP, settings, async (_, x, c) =>
                //{
                //    var msg = x.OriginalEvent.FullEvent.Value;
                //    if (msg is Cat cat)
                //    {
                //        Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //    }
                //    if (msg is Dog dog)
                //    {
                //        Console.WriteLine("Received1: " + x.Event.EventStreamId + ":" + x.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    await Task.Delay(500);
                //},
                //(subscription, reason, exc) =>
                //{
                //    Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
                //});

                #endregion

                #region VolatileSubscription

                //var settings = new SubscriptionSettings();
                ////var settings = new SubscriptionSettings { MaxDegreeOfParallelismPerBlock = 5 };
                ////var settings = new SubscriptionSettings { BoundedCapacityPerBlock = 2, NumActionBlocks = 5 };

                //var subscription = new VolatileSubscription(STREAM);
                //subscription.Settings = settings;
                //var volatileConsumer = new VolatileConsumer();
                //volatileConsumer.Initialize(conn, subscription, addEventHandlers: _ =>
                //   _.Add<Cat>(iEvent =>
                //   {
                //       if ((iEvent.OriginalEventNumber % 2) == 0 && !throwEx)
                //       {
                //           throwEx = true;
                //           throw new Exception();
                //       }
                //       var cat = iEvent.OriginalEvent.FullEvent.Value;
                //       Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //   }
                //   ).Add<Dog>(iEvent =>
                //   {
                //       if ((iEvent.OriginalEventNumber % 9) == 0)
                //       {
                //           conn.Close();
                //           //volatileConsumer.Dispose();
                //       }
                //       var dog = iEvent.OriginalEvent.FullEvent.Value;
                //       Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //   }
                //   ));
                //volatileConsumer.ConnectToSubscriptionAsync();

                //var sub = conn.VolatileSubscribeAsync(STREAM, settings,
                //    addEventHandlers: _ =>
                //    _.Add<Cat>(iEvent =>
                //      {
                //          var cat = iEvent.OriginalEvent.FullEvent.Value;
                //          Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //      }
                //    ).Add<Dog>(iEvent =>
                //    {
                //        var dog = iEvent.OriginalEvent.FullEvent.Value;
                //        Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    ),
                //    subscriptionDropped: (subscription, reason, exc) =>
                //    {
                //        Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
                //    });

                //var sub = conn.VolatileSubscribeAsync(STREAM, settings,
                //    addHandlers: _ =>
                //    _.Add<Cat>(cat =>
                //      {
                //          Console.WriteLine("Received Cat: " + cat.Name + ":" + cat.Meow);
                //      }
                //    ).Add<Dog>(dog =>
                //    {
                //        Console.WriteLine("Received Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    ),
                //    subscriptionDropped: (subscription, reason, exc) =>
                //    {
                //        Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
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

                //var subscription = new CatchUpSubscription(STREAM);
                //subscription.Settings = settings;
                //var catchUpConsumer = new CatchUpConsumer();
                //catchUpConsumer.Initialize(conn, subscription, addEventHandlers: _ =>
                //    _.Add<Cat>(iEvent =>
                //    {
                //        if (iEvent.OriginalEventNumber == 2 && !throwEx)
                //        {
                //            throwEx = true;
                //            //throw new Exception();
                //        }
                //        var cat = iEvent.OriginalEvent.FullEvent.Value;
                //        Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //    }
                //    ).Add<Dog>(iEvent =>
                //    {
                //        if (iEvent.OriginalEventNumber == 9)
                //        {
                //            conn.Close();
                //            //catchUpConsumer.Dispose();
                //        }
                //        var dog = iEvent.OriginalEvent.FullEvent.Value;
                //        Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    ));
                //catchUpConsumer.ConnectToSubscriptionAsync();

                //var sub = conn.CatchUpSubscribe(STREAM, StreamCheckpoint.StreamStart, settings,
                //    addEventHandlers: _ =>
                //    _.Add<Cat>(iEvent =>
                //      {
                //          var cat = iEvent.OriginalEvent.FullEvent.Value;
                //          Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
                //      }
                //    ).Add<Dog>(iEvent =>
                //    {
                //        var dog = iEvent.OriginalEvent.FullEvent.Value;
                //        Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    ),
                //    subscriptionDropped: (subscription, reason, exc) =>
                //    {
                //        Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
                //    });

                //var sub = conn.CatchUpSubscribe(STREAM, 1000, settings,
                //    addHandlers: _ =>
                //    _.Add<Cat>(cat =>
                //      {
                //          Console.WriteLine("Received Cat: " + cat.Name + ":" + cat.Meow);
                //      }
                //    ).Add<Dog>(dog =>
                //    {
                //        Console.WriteLine("Received Dog: " + dog.Name + ":" + dog.Bark);
                //    }
                //    ),
                //    subscriptionDropped: (subscription, reason, exc) =>
                //    {
                //        Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
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

                Console.WriteLine("waiting for events. press any key to close connection");
                Console.ReadKey();
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void UpdateSubscription(IEventStoreConnection conn)
        {
            PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
                .DoNotResolveLinkTos()
                .StartFromBeginning();

            conn.UpdateOrCreatePersistentSubscription(STREAM, GROUP, settings);
        }

        private static void UpdateStreamMetadata(IEventStoreConnection conn)
        {
            conn.SetStreamMetadataAsync(STREAM, ExpectedVersion.Any, StreamMetadata.Create(1000, TimeSpan.FromMinutes(30)))
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
