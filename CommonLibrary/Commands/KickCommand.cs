

namespace ServerCommon.Commands
{
    public class KickCommand : IServerCommand
    {
        public string Name => "kick";
        public string Description => "Kicks a player from the server.";
        public string Usage => "/kick <id> [reason]";
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

                string reason = context.Args.Length > 1
                    ? string.Join(" ", context.Args.Skip(1))
                    : "Kicked by admin";

                string kickedPlayerName = context.Server.KickPlayer(id, reason);

                if (kickedPlayerName == null)
                   
                    context.Reply($"Player {id} not found.", LogType.Warning);
            }
            else
            {
                context.Reply("Invalid ID format.", LogType.Error);
            }
        }
    }
}