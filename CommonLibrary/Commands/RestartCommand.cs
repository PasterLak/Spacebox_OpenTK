

namespace ServerCommon.Commands
{
    public class RestartCommand : IServerCommand
    {
        public string Name => "restart";
        public string Description => "Restarts the server network interface.";
        public string Usage => "/restart";
        public string[] Aliases => new[] { "reload" };

        public void Execute(CommandContext context)
        {
            context.Server.Restart();
            context.Reply("Server network restarted.", LogType.Success);
        }
    }

}
