using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace ES.Producer
{
  class Program
  {
    static void Main(string[] args)
    {
      const string STREAM = "a_test_stream";
      const int DEFAULTPORT = 1113;
      //uncomment to enable verbose logging in client.
      var settings = ConnectionSettings.Create();//.EnableVerboseLogging().UseConsoleLogger();
      using (var conn = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Loopback, DEFAULTPORT)))
      {
        conn.ConnectAsync().Wait();
        for (var x = 0; x < 100; x++)
        {
          conn.AppendToStreamAsync(STREAM,
              ExpectedVersion.Any,
              GetEventDataFor(x)).Wait();
          Console.WriteLine("event " + x + " written.");
          Thread.Sleep(200);
        }
      }

      Console.WriteLine("按任意键退出！");
      Console.ReadKey();
    }

    private static EventData GetEventDataFor(int i)
    {
      return new EventData(
          Guid.NewGuid(),
          "eventType",
          true,
          Encoding.ASCII.GetBytes("{\"somedata\" : " + i + "}"),
          Encoding.ASCII.GetBytes("{\"metadata\" : " + i + "}")
          );
    }
  }
}
