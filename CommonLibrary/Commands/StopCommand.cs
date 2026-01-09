using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon.Commands
{
    public class StopCommand : IServerCommand
    {
        private readonly CommandProcessor _processor;
        public string Name => "stop";
        public string Description => "Stops the server gracefully.";
        public string Usage => "/stop";
        public string[] Aliases => new[] { "shutdown", "exit" };

        public StopCommand(CommandProcessor processor) => _processor = processor;

        public void Execute(CommandContext context)
        {
            context.Reply("Stopping server...", LogType.Warning);
            context.Server.BroadcastChat(-1, "Server is shutting down...");
            _processor.OnStop?.Invoke();
            _processor.Server.Stop();

        }
    }
}
