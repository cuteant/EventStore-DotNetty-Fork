using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Es.Receiver
{
  class Program
  {
    const string STREAM = "a_test_stream";
    const string GROUP = "a_test_group";
    const int DEFAULTPORT = 1113;

    static void Main(string[] args)
    {
      //uncommet to enable verbose logging in client.
      var settings = ConnectionSettings.Create();//.EnableVerboseLogging().UseConsoleLogger();

      //using (var conn = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Loopback, DEFAULTPORT)))
      //{
      //  conn.ConnectAsync().Wait();

      //  conn.DeleteStreamAsync(STREAM, ExpectedVersion.Any, new UserCredentials("admin", "changeit"));

      //  Console.WriteLine("waiting for events. press enter to exit");
      //  Console.ReadKey();
      //  return;
      //}

      using (var conn = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Loopback, DEFAULTPORT)))
      {
        conn.ConnectAsync().Wait();

        var eventSlice = conn.ReadStreamEventsForwardAsync(STREAM, 0, 100, true, new UserCredentials("admin", "changeit")).GetAwaiter().GetResult();

        Console.WriteLine("FromEventNumber: {0}", eventSlice.FromEventNumber);
        Console.WriteLine("NextEventNumber: {0}", eventSlice.NextEventNumber);
        Console.WriteLine("LastEventNumber: {0}", eventSlice.LastEventNumber);
        Console.WriteLine("IsEndOfStream: {0}", eventSlice.IsEndOfStream);

        foreach (var x in eventSlice.Events)
        {
          var data = Encoding.ASCII.GetString(x.Event.Data);
          Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
          Console.WriteLine(data);
        }

        Console.WriteLine("waiting for events. press enter to exit");
        Console.ReadKey();
      }

    }
  }
}
