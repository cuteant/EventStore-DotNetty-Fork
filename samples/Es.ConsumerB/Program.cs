using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.SystemData;

namespace Es.Consumer
{
  class Program
  {
    const string STREAM = "a_test_stream";
    const string GROUP = "a_test_group_2";
    const int DEFAULTPORT = 1113;

    static void Main(string[] args)
    {

      //uncommet to enable verbose logging in client.
      var settings = ConnectionSettings.Create();//.EnableVerboseLogging().UseConsoleLogger();
      using (var conn = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Loopback, DEFAULTPORT)))
      {
        conn.ConnectAsync().Wait();

        //Normally the creating of the subscription group is not done in your general executable code. 
        //Instead it is normally done as a step during an install or as an admin task when setting 
        //things up. You should assume the subscription exists in your code.
        CreateSubscription(conn);
        //UpdateSubscription(conn);

        conn.ConnectToPersistentSubscription(STREAM, GROUP, (_, x) =>
        {
          var data = Encoding.ASCII.GetString(x.Event.Data);
          Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
          Console.WriteLine(data);
        }, null, null, 10, true);
        conn.ConnectToPersistentSubscription(STREAM, GROUP, (_, x) =>
        {
          var data = Encoding.ASCII.GetString(x.Event.Data);
          Console.WriteLine("2 Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
          Console.WriteLine(data);
        }, null, null, 10, true);

        #region VolatileSubscription
        //var sub = conn.SubscribeToStreamAsync(STREAM, true,
        //    (_, x) =>
        //    {
        //      var data = Encoding.ASCII.GetString(x.Event.Data);
        //      Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //      Console.WriteLine(data);
        //    });
        //var sub1 = conn.SubscribeToStreamAsync(STREAM, true,
        //    (_, x) =>
        //    {
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

        //var sub = conn.SubscribeToStreamFrom(STREAM, null, true,
        //    (_, x) =>
        //    {
        //      var data = Encoding.ASCII.GetString(x.Event.Data);
        //      Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //      Console.WriteLine(data);
        //    });
        //var sub1 = conn.SubscribeToStreamFrom(STREAM, StreamPosition.Start, true,
        //    (_, x) =>
        //    {
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

      try
      {
        conn.CreatePersistentSubscriptionAsync(STREAM, GROUP, settings, new UserCredentials("admin", "changeit")).Wait();
      }
      catch (AggregateException ex)
      {
        if (ex.InnerException.GetType() != typeof(InvalidOperationException)
            && ex.InnerException?.Message != $"Subscription group {GROUP} on stream {STREAM} already exists")
        {
          throw;
        }
      }
    }

    private static void UpdateSubscription(IEventStoreConnection conn)
    {
      PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
          .DoNotResolveLinkTos()
          .StartFromCurrent();

      try
      {
        conn.UpdatePersistentSubscriptionAsync(STREAM, GROUP, settings, new UserCredentials("admin", "changeit")).Wait();
      }
      catch (AggregateException ex)
      {
        if (ex.InnerException.GetType() != typeof(InvalidOperationException)
            && ex.InnerException?.Message != $"Subscription group {GROUP} on stream {STREAM} already exists")
        {
          throw;
        }
      }
    }
  }
}
