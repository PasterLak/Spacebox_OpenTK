using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon.Commands
{
    public class BanCommand : IServerCommand
    {
        public string Name => "ban";
        public string Description => "Bans a player by ID.";
        public string Usage => "/ban <id> [reason]";
        public string[] Aliases => Array.Empty<string>();

        public void Execute(CommandContext context)
        {
            if (context.Args.Length < 1)
            {
                context.Reply($"Usage: {Usage}", LogType.Warning);
                return;
            }

            if (int.TryParse(context.Args[0], out int id))
            {
                string reason = context.Args.Length > 1 ? string.Join(" ", context.Args.Skip(1)) : "Banned by admin";
                bool result = context.Server.BanPlayer(id, reason);

                if (result)
                    context.Reply($"Player {id} banned. Reason: {reason}", LogType.Success);
                else
                    context.Reply($"Player {id} not found.", LogType.Warning);
            }
            else
            {
                context.Reply("Invalid ID format.", LogType.Error);
            }
        }
    }
}
