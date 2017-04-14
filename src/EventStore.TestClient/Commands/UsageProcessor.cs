using System.Linq;
using Microsoft.Extensions.Logging;

namespace EventStore.TestClient.Commands
{
    internal class UsageProcessor : ICmdProcessor
    {
        public string Keyword { get { return "USAGE"; } }
        public string Usage { get { return "USAGE"; } }

        private readonly CommandsProcessor _commands;

        public UsageProcessor(CommandsProcessor commands)
        {
            _commands = commands;
        }

        public bool Execute(CommandProcessorContext context, string[] args)
        {
            var allCommands = string.Join("\n\n", _commands.RegisteredProcessors.Select(x => x.Usage.ToUpper()));
            context.Log.LogInformation("Available commands:\n{0}", allCommands);
            return true;
        }
    }
}