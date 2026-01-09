using System.Text;
using ServerCommon;
using SpaceNetwork;

namespace ServerCommon.Commands
{
    public class ServerInfoCommand : IServerCommand
    {
        public string Name => "serverinfo";
        public string Description => "Displays current server settings and configuration.";
        public string Usage => "/info";
        public string[] Aliases => new[] { "serverinfo", "config" };

        public void Execute(CommandContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Server Information ===");
            sb.AppendLine($"Name:           {Settings.Name}");
            sb.AppendLine($"Description:    {Settings.Description}");
            sb.AppendLine($"IP Address:     {Settings.Ip}");
            sb.AppendLine($"Port:           {Settings.Port}");
            sb.AppendLine($"Max Players:    {Settings.MaxPlayers}");
            sb.AppendLine($"App Key:        {(string.IsNullOrEmpty(Settings.Key) ? "None" : Settings.Key)}");
            sb.AppendLine("--- Mod Settings ---");
            sb.AppendLine($"Mod Folder:     {Settings.ModFolder}");
            sb.AppendLine($"Mod Hash:       {(string.IsNullOrEmpty(Settings.ModFolderHash) ? "Not Calculated" : Settings.ModFolderHash.Substring(0, Math.Min(8, Settings.ModFolderHash.Length)) + "...")}");
            sb.AppendLine("--- Network Config ---");
            sb.AppendLine($"Ping Interval:  {Settings.PingInterval}s");
            sb.AppendLine($"Timeout:        {Settings.ConnectionTimeout}s");
            sb.AppendLine($"AFK Check:      Every {Settings.TimeToCheckAfk}s (Kick after {Settings.TimeCanBeAfkSec}s)");

            context.Reply(sb.ToString(), LogType.Info);
        }
    }
}