using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon.Commands
{
    public class ListPlayersCommand : IServerCommand
    {
        public string Name => "list";
        public string Description => "Lists all connected players.";
        public string Usage => "/list";
        public string[] Aliases => new[] { "players", "online" };

        public void Execute(CommandContext context)
        {
            var players = context.PlayerManager.GetAll();
            if (players.Count == 0)
            {
                context.Reply("No players online.", LogType.Info);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Online Players ({players.Count}):");
            foreach (var p in players.Values)
            {
                sb.AppendLine($"  ID: {p.ID} | Name: {p.Name} | IP: {context.Server.GetPlayerIp(p.ID)}");
            }
            context.Reply(sb.ToString());
        }
    }

}
