

namespace ServerCommon.Commands
{
    public class SayCommand : IServerCommand
    {
        public string Name => "say";
        public string Description => "Sends a chat message to all players as [Server].";
        public string Usage => "/say <message>";
        public string[] Aliases => new[] { "broadcast" , "s"};

        public void Execute(CommandContext context)
        {
            if (context.Args.Length < 1)
            {
                context.Reply($"Usage: {Usage}", LogType.Warning);
                return;
            }

            string msg = context.RawArgs;
            context.Server.BroadcastChat(-1, msg);
            context.Reply($"[Broadcast]: {msg}");
        }
    }
}
