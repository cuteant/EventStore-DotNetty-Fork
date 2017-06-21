using System;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Es.Consumer
{
  class Program
  {
    const string STREAM = "test-animal";

    static void Main(string[] args)
    {


      var connStr = "ConnectTo=tcp://admin:changeit@localhost:1113";
      var connSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying().EnableVerboseLogging().UseFileLogger("my.log");
      using (var conn = EventStoreConnection.Create(connStr, connSettings))
      {
        conn.ConnectAsync().Wait();

        #region CatchupSubscription
        //Note the subscription is subscribing from the beginning every time. You could also save
        //your checkpoint of the last seen event and subscribe to that checkpoint at the beginning.
        //If stored atomically with the processing of the event this will also provide simulated
        //transactional messaging.

        var settings = new CatchUpSubscriptionSettings(10000, 20, true, true);

        //settings.MaxDegreeOfParallelismPerBlock = 5;

        //settings.BoundedCapacityPerBlock = 2;
        //settings.NumActionBlocks = 5;

        var sub = conn.SubscribeToStreamFrom(STREAM, 90, settings,
            eventAppeared: async (_, x) =>
            {
              var data = Encoding.ASCII.GetString(x.Event.Data);
              //if (x.Event.EventNumber % 3 == 0)
              //{
              //  var errorMsg = $"error event number: {x.Event.EventNumber}";
              //  Console.WriteLine(errorMsg);
              //  throw new InvalidOperationException(errorMsg);
              //}
              Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
              Console.WriteLine(data);
              await Task.Delay(500);
            },
            subscriptionDropped: (subscription, reason, exc) =>
            {
              Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
            });

        #endregion

        Console.WriteLine("waiting for events. press enter to exit");
        Console.ReadKey();
      }
    }
  }
}
