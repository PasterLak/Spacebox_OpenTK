
using ServerCommon;


namespace SpaceServer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("|-----------[SPACEBOX SERVER]-----------|");

            

            ILogger logger = new ConsoleLogger();

            ServerPreparer.IsServerReady(logger);

            var server = new ServerNetwork(Settings.Key, Settings.Port, Settings.MaxPlayers, logger);
            var commandProcessor = new CommandProcessor(server, logger);

            commandProcessor.OnClear += () => { Console.Clear(); };

            new Thread(() => server.RunMainLoop()).Start();

            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Clear();
                }
                else
                {
                    commandProcessor.ProcessCommand(line);
                }
            }
        }
    }
}
