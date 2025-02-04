using System;
using ServerCommon;

namespace ServerCommon
{
    public class CommandProcessor
    {
        private readonly ServerNetwork server;
        private readonly Action<string> logCallback;
        public Action OnClear;
        public CommandProcessor(ServerNetwork server, Action<string> logCallback)
        {
            this.server = server;
            this.logCallback = logCallback;
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
                    logCallback?.Invoke(kicked ? $"[Server]: Player {id} kicked." : $"[Server]: Player {id} not found.");
                }
                else
                {
                    logCallback?.Invoke("[Server]: Invalid kick command format.");
                }
            }
            else if (input.StartsWith("ban ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
                {
                    string reason = parts.Length == 3 ? parts[2] : "No reason provided";
                    bool banned = server.BanPlayer(id, reason);
                    logCallback?.Invoke(banned ? $"[Server]: Player {id} banned. Reason: {reason}" : $"[Server]: Player {id} not found.");
                }
                else
                {
                    logCallback?.Invoke("[Server]: Invalid ban command format.");
                }
            }
            else if (input.Equals("restart", StringComparison.OrdinalIgnoreCase))
            {
                server.Restart();
                logCallback?.Invoke("[Server]: Server restarted.");
            }
            else
            {
                server.BroadcastChat(-1, input);
                logCallback?.Invoke($"[Server]: {input}");
            }
        }
    }
}
