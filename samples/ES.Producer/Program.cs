using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Es.SharedModels;

namespace ES.Producer
{
  class Program
  {
    const string STREAM = "a_test_stream";
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

        #region AppendToStreamAsync 
        //for (var x = 0; x < 100; x++)
        //{
        //  conn.AppendToStreamAsync(STREAM,
        //      ExpectedVersion.Any,
        //      GetEventDataFor(x)).Wait();
        //  Console.WriteLine("event " + x + " written.");
        //  Thread.Sleep(100);
        //}
        #endregion

        #region SendAsync
        conn.SendAsync("a_test_stream_sending", new TestMessage { Id = 10, Text = "this is a test." });
        conn.SendAsync("a_test_stream_animal", new Cat { Name = "MyCat", Meow = "meowing......" });
        conn.SendAsync("a_test_stream_animal", new Dog { Name = "MyCat", Bark = "barking......" });
        #endregion
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
