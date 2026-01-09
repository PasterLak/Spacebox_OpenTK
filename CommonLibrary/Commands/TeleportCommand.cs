using System;
using System.Numerics;
using ServerCommon;
using SpaceNetwork;

namespace ServerCommon.Commands
{
    public class TeleportCommand : IServerCommand
    {
        public string Name => "tp";
        public string Description => "Teleports a player to coordinates.";
        public string Usage => "/tp <player_id> <x> <y> <z>";
        public string[] Aliases => new[] { "teleport" };

        public void Execute(CommandContext context)
        {
            if (context.Args.Length < 4)
            {
                context.Reply($"Usage: {Usage}", LogType.Warning);
                return;
            }

            if (!int.TryParse(context.Args[0], out int playerId))
            {
                context.Reply("Invalid Player ID.", LogType.Error);
                return;
            }

            var players = context.PlayerManager.GetAll();
            if (!players.TryGetValue(playerId, out var player))
            {
                context.Reply("Player not found.", LogType.Warning);
                return;
            }

            if (float.TryParse(context.Args[1], out float x) &&
                float.TryParse(context.Args[2], out float y) &&
                float.TryParse(context.Args[3], out float z))
            {
                player.Position = new Vector3(x, y, z);

                context.Server.SendPositionUpdate(player);
                context.Reply($"Teleported {player.Name} to {x}, {y}, {z}.", LogType.Success);
            }
            else
            {
                context.Reply("Invalid coordinates.", LogType.Error);
            }
        }
    }
}