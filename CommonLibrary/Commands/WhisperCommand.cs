using System;
using System.Linq;
using ServerCommon;
using SpaceNetwork;

namespace ServerCommon.Commands
{
    public class WhisperCommand : IServerCommand
    {
        public string Name => "msg";
        public string Description => "Sends a private message to a player.";
        public string Usage => "/msg <player_id> <message>";
        public string[] Aliases => new[] { "tell", "w", "whisper" };

        public void Execute(CommandContext context)
        {
            if (context.Args.Length < 2)
            {
                context.Reply($"Usage: {Usage}", LogType.Warning);
                return;
            }

            if (int.TryParse(context.Args[0], out int targetId))
            {
                var players = context.PlayerManager.GetAll();
                if (players.TryGetValue(targetId, out var targetPlayer))
                {
                    string message = string.Join(" ", context.Args.Skip(1));
                    string formatMsg = $"[Private from Server]: {message}";

                    context.Server.SendPrivateMessage(targetPlayer.ID, formatMsg);
                    context.Reply($"Message sent to {targetPlayer.Name}.", LogType.Info);
                }
                else
                {
                    context.Reply("Player not found.", LogType.Warning);
                }
            }
            else
            {
                context.Reply("Invalid Player ID format.", LogType.Error);
            }
        }
    }
}