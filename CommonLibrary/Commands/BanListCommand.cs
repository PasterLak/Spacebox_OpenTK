using System.Linq;
using System.Text;
using ServerCommon;
using SpaceNetwork;

namespace ServerCommon.Commands
{
    public class BanListCommand : IServerCommand
    {
        public string Name => "banlist";
        public string Description => "Shows a list of all banned players.";
        public string Usage => "/banlist";
        public string[] Aliases => new[] { "bans" };

        public void Execute(CommandContext context)
        {
            var bans = BanManager.GetAllBanned();

            if (bans == null || !bans.Any())
            {
                context.Reply("No banned players found.", LogType.Info);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"--- Banned Players ({bans.Count()}) ---");

            foreach (var ban in bans)
            {
                sb.AppendLine($"Name: {ban.Name} | IP: {ban.IPAddress} | Reason: {ban.Reason} | Date: {ban.BannedAt.ToShortDateString()}");
            }

            context.Reply(sb.ToString());
        }
    }
}