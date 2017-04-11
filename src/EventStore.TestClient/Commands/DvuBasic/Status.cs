﻿using EventStore.Common.Logging;
using EventStore.Common.Utils;

namespace EventStore.TestClient.Commands.DvuBasic
{
    public class Status
    {
        private readonly ILogger _log;

        public int ThreadId { get; private set; }
        public bool Success { get; private set; }

        public Status(ILogger log)
        {
            Ensure.NotNull(log, "log");
            _log = log;
        }

        public void ReportWritesProgress(int threadId,
                                         int sent,
                                         int prepareTimeouts,
                                         int commitTimeouts,
                                         int forwardTimeouts,
                                         int wrongExpctdVersions,
                                         int streamsDeleted,
                                         int fails,
                                         int requests)
        {
            var sentP = ToPercent(sent, requests);
            var failsP = ToPercent(fails, sent);

            var table = new ConsoleTable("WRITER ID", "Completed %", "Completed/Total",
                                         "Failed %", "Failed/Sent", "Prepare Timeouts",
                                         "Commit Timeouts", "Forward Timeouts",
                                         "Wrong Versions", "Stream Deleted");
            table.AppendRow(threadId.ToString(), string.Format("{0:0.0}%", sentP), string.Format("{0}/{1}", sent, requests),
                            string.Format("{0:0.0}%", failsP), string.Format("{0}/{1}", fails, sent), prepareTimeouts.ToString(),
                            commitTimeouts.ToString(), forwardTimeouts.ToString(),
                            wrongExpctdVersions.ToString(), streamsDeleted.ToString());

            if (failsP > 50d)
                _log.Fatal(table.CreateIndentedTable());
            else
                _log.Info(table.CreateIndentedTable());
        }

        public void ReportReadsProgress(int threadId, int successes, int fails)
        {
            var all = successes + fails;

            var table = new ConsoleTable("READER ID", "Fails", "Total Random Reads");
            table.AppendRow(threadId.ToString(), fails.ToString(), all.ToString());

            if (fails != 0)
                _log.Fatal(table.CreateIndentedTable());
            else
                _log.Info(table.CreateIndentedTable());
        }

        public void ReportReadError(int threadId, string stream, int indx)
        {
            _log.Fatal("FATAL : READER [{0}] encountered an error in {1} ({2})", threadId, stream, indx);
        }

        public void ReportNotFoundOnRead(int threadId, string stream, int indx)
        {
            _log.Fatal("FATAL : READER [{0}] asked for event {1} in '{2}' but server returned 'Not Found'", threadId,
                       indx, stream);
        }

        public void FinilizeStatus(int threadId, bool success)
        {
            ThreadId = threadId;
            Success = success;
        }

        private double ToPercent(int value, int max)
        {
            var approx = (value / (double)max) * 100d;
            return approx <= 100d ? approx : 100;
        }
    }
}