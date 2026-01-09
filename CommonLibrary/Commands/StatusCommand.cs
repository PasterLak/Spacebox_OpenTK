using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon.Commands
{

    public class StatusCommand : IServerCommand
    {
        public string Name => "status";
        public string Description => "Shows server performance statistics.";
        public string Usage => "/status";
        public string[] Aliases => new[] { "stats", "info" };

        public void Execute(CommandContext context)
        {
            int players = context.PlayerManager.GetAll().Count;
          
            context.Reply($"--- Server Status ---\nPlayers: {players}\nMax Players: {Settings.MaxPlayers}\nStatus: Running", LogType.Info);
        }
    }
}
