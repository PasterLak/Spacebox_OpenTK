using System;
using System.Linq;
using ServerCommon;
using SpaceNetwork;

namespace ServerCommon.Commands
{
    public class UnbanCommand : IServerCommand
    {
        public string Name => "unban";
        public string Description => "Unbans a player by name or IP.";
        public string Usage => "/unban <name/ip>";
        public string[] Aliases => new[] { "pardon" };

        public void Execute(CommandContext context)
        {
            if (context.Args.Length < 1)
            {
                context.Reply($"Usage: {Usage}", LogType.Warning);
                return;
            }

            string target = context.Args[0];

            if (BanManager.RemoveBannedPlayer(target))
            {
                context.Reply($"Unbanned {target}.", LogType.Success);
            }
            else
            {
                context.Reply($"Could not find ban record for {target}.", LogType.Warning);
            }
        }
    }
}