using System;
using Es.SharedModels;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

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
                conn.Connect();

                try
                {
                    //var eventSlice = conn.GetStreamEventsForward<IAnimal>(0, 2000, true);
                    var eventSlice = conn.GetStreamEventsBackward<IAnimal>(StreamPosition.End, 2000, true);

                    Console.WriteLine("FromEventNumber: {0}", eventSlice.FromEventNumber);
                    Console.WriteLine("NextEventNumber: {0}", eventSlice.NextEventNumber);
                    Console.WriteLine("LastEventNumber: {0}", eventSlice.LastEventNumber);
                    Console.WriteLine("IsEndOfStream: {0}", eventSlice.IsEndOfStream);

                    foreach (var x in eventSlice.Events)
                    {
                        Console.WriteLine("Received: " + x.OriginalEvent.EventStreamId + ":" + x.OriginalEvent.EventNumber);
                        Console.WriteLine(x.OriginalEvent.FullEvent.Value.Name);
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
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
