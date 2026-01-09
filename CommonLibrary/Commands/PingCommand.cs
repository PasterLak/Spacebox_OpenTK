using System;
using System.Linq;
using ServerCommon;
using SpaceNetwork;

namespace ServerCommon.Commands
{
    public class PingCommand : IServerCommand
    {
        public string Name => "ping";
        public string Description => "Shows the network latency (RTT) of a player.";
        public string Usage => "/ping <id>";
        public string[] Aliases => new[] { "latency", "rtt" };

        public void Execute(CommandContext context)
        {
            if (context.Args.Length < 1)
            {
                context.Reply($"Usage: {Usage}", LogType.Warning);
                return;
            }

            if (!int.TryParse(context.Args[0], out int id))
            {
                context.Reply("Invalid Player ID format.", LogType.Error);
                return;
            }

            var players = context.PlayerManager.GetAll();
            if (!players.TryGetValue(id, out var player))
            {
                context.Reply($"Player {id} not found.", LogType.Warning);
                return;
            }

            float rttSeconds = context.Server.GetPlayerPing(id);

            if (rttSeconds < 0)
            {
                context.Reply($"Connection for player {player.Name} not found.", LogType.Error);
                return;
            }

            int ms = (int)(rttSeconds * 1000);
            string status = ms < 60 ? "Excellent" : (ms < 150 ? "Good" : (ms < 300 ? "Poor" : "Bad"));

            context.Reply($"Ping for {player.Name}: {ms}ms [{status}]", LogType.Info);
        }
    }
}