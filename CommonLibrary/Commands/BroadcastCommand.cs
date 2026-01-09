using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon.Commands
{
    public class BroadcastCommand : IServerCommand
    {
        public string Name => "alert";
        public string Description => "Sends an alert message.";
        public string Usage => "/alert <message>";
        public string[] Aliases => Array.Empty<string>();

        public void Execute(CommandContext context)
        {
            if (context.Args.Length < 1) return;
            string msg = "[ALERT]: " + context.RawArgs;
            context.Server.BroadcastChat(-1, msg);
            context.Reply(msg, LogType.Warning);
        }
    }
}
