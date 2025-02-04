using System;
using ServerCommon;

namespace ServerCommon
{
    public class CommandProcessor
    {
        private readonly ServerNetwork _server;
        private readonly Action<string> _logCallback;
        public Action OnClear;
        public CommandProcessor(ServerNetwork server, Action<string> logCallback)
        {
            _server = server;
            _logCallback = logCallback;
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
                    bool kicked = _server.KickPlayer(id);
                    if (kicked)
                        _logCallback?.Invoke($"[Server]: Player {id} kicked.");
                    else
                        _logCallback?.Invoke($"[Server]: Player {id} not found.");
                }
                else
                {
                    _logCallback?.Invoke("[Server]: Invalid kick command format.");
                }
            }
            else if (input.StartsWith("ban ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
                {
                    string reason = parts.Length == 3 ? parts[2] : "No reason provided";
                    bool banned = _server.BanPlayer(id, reason);
                    if (banned)
                        _logCallback?.Invoke($"[Server]: Player {id} banned. Reason: {reason}");
                    else
                        _logCallback?.Invoke($"[Server]: Player {id} not found.");
                }
                else
                {
                    _logCallback?.Invoke("[Server]: Invalid ban command format.");
                }
            }
            else if (input.Equals("restart", StringComparison.OrdinalIgnoreCase))
            {
                _server.Restart();
                _logCallback?.Invoke("[Server]: Server restarted.");
            }
            else
            {
                _server.BroadcastChat(-1, input);
                _logCallback?.Invoke($"[Server]: {input}");
            }
        }
    }
}
