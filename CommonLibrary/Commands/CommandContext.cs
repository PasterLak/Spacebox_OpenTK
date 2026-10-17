

namespace ServerCommon.Commands
{
    public class CommandContext
    {
        public ServerNetwork Server { get; }
        public ILogger Logger { get; }
        public PlayerManager PlayerManager { get; }
        public string[] Args { get; }
        public string RawArgs { get; }

        public CommandContext(ServerNetwork server, ILogger logger, string[] args, string rawArgs)
        {
            Server = server;
            Logger = logger;
            PlayerManager = Server.PlayerManager;
            Args = args;
            RawArgs = rawArgs;
        }

        public void Reply(string message, LogType type = LogType.Normal)
        {
            Logger.Log(message, type);
        }
    }

}
