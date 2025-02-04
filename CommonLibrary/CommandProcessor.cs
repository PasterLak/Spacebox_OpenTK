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
                // Здесь можно инициировать очистку логов в UI. 
                // Для консоли можно, например, вызвать Console.Clear();
                // Для WPF – поднять событие или напрямую вызвать метод обновления UI.
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
