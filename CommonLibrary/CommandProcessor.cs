using System;
using ServerCommon;
using SpaceNetwork;

namespace ServerCommon
{
    public class CommandProcessor
    {
        private readonly ServerNetwork server;
        private readonly ILogger logger;
        public Action OnClear;

        public CommandProcessor(ServerNetwork server, ILogger logger)
        {
            this.server = server;
            this.logger = logger;
        }

        public void ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                OnClear?.Invoke();
            }
            else if (input.StartsWith("kick ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ');
                if (parts.Length == 2 && int.TryParse(parts[1], out int id))
                {
                    bool kicked = server.KickPlayer(id);
                    if (!kicked)
                        logger.Log($"Player {id} not found.", LogType.Warning);
                    else
                        logger.Log($"Player {id} kicked.", LogType.Info);
                }
                else
                {
                    logger.Log("[Server]: Invalid kick command format.", LogType.Error);
                }
            }
            else if (input.StartsWith("ban ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
                {
                    string reason = parts.Length == 3 ? parts[2] : "No reason provided";
                    bool banned = server.BanPlayer(id, reason);
                    if (!banned)
                        logger.Log($"Player {id} not found.", LogType.Warning);
                    else
                        logger.Log($"Player {id} banned. Reason: {reason}", LogType.Info);
                }
                else
                {
                    logger.Log("[Server]: Invalid ban command format.", LogType.Error);
                }
            }
            else if (input.Equals("restart", StringComparison.OrdinalIgnoreCase))
            {
                server.Restart();
                logger.Log("[Server]: Server restarted.", LogType.Success);
            }
            else
            {
                server.BroadcastChat(-1, input);
                logger.Log($"[Server]: {input}", LogType.Normal);
            }
        }
    }
}
