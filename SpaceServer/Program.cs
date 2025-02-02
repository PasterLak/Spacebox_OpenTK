using System;
using System.Threading;
using SpaceNetwork;

namespace SpaceServer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("|-----------[SPACEBOX SERVER]-----------|");
            var data = KeyValueFileReader.ReadFile("config.txt");
            Settings.Key = data["key"];
            Settings.Port = int.Parse(data["port"]);
            Settings.ConnectionTimeout = int.Parse(data["connectionTimeout"]);
            Settings.PingInterval = float.Parse(data["pingInterval"]);
            int maxPlayers = int.Parse(data["maxPlayers"]);
            var server = new ServerNetwork(Settings.Key, Settings.Port, maxPlayers, Console.WriteLine);
            new Thread(() => server.RunMainLoop()).Start();
            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (string.Equals(line, "clear", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Clear();
                }
                else if (line.StartsWith("kick "))
                {
                    var parts = line.Split(' ');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int id))
                    {
                        bool kicked = server.KickPlayer(id);
                        if (kicked)
                            Console.WriteLine($"[Server]: Player {id} kicked.");
                    }
                    else
                    {
                        Console.WriteLine("[Server]: Invalid kick command format.");
                    }
                }
                else if (string.Equals(line, "restart", StringComparison.OrdinalIgnoreCase))
                {
                    server.Restart();
                }
                else
                {
                    server.BroadcastChat(-1, line);
                    Console.WriteLine($"[Server]: {line}");
                }
            }
        }
    }
}
