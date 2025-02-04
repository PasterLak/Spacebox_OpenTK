using System;

namespace ServerCommon
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message, LogType type)
        {

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColor(type);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {message}");
            Console.ForegroundColor = originalColor;
        }

        private ConsoleColor GetColor(LogType type)
        {
            switch (type)
            {
                case LogType.Error: return ConsoleColor.Red;
                case LogType.Success: return ConsoleColor.Green;
                case LogType.Warning: return ConsoleColor.Yellow;
                case LogType.Info: return ConsoleColor.Cyan;
                default: return ConsoleColor.White;
            }
        }
    }
}
