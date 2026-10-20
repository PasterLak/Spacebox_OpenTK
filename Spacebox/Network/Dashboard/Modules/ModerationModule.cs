using Engine;
using Engine.Web;
using Spacebox.Network.Moderation;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class ModerationModule : IDashboardModule
{
    private sealed class AddressRequest
    {
        public string Address { get; set; }
    }

    private sealed class ToggleRequest
    {
        public bool Enabled { get; set; }
    }

    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;
        var config = context.Config;

        router.Map("GET", DashboardContext.Api("/moderation"), guard.Admin(async (_, _) =>
        {
            var data = await MainThread.Run(ServerModeration.Snapshot);
            return HttpResponse.Json(new
            {
                bannedNames = data.BannedNames,
                bannedAddresses = data.BannedAddresses,
                mutedNames = data.MutedNames,
                whitelistEnabled = config.WhitelistEnabled,
                whitelist = config.Whitelist
            });
        }));

        router.Map("POST", DashboardContext.Api("/moderation/unban"), guard.Admin(async (request, user) =>
        {
            string name = DashboardGuard.Read<NameRequest>(request)?.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return HttpResponse.Error("Missing name", 400);

            bool removed = await MainThread.Run(() => ServerModeration.Unban(name));
            if (removed) DashboardContext.Audit(user, $"unbanned {name}");
            return HttpResponse.Json(new { ok = removed });
        }));

        router.Map("POST", DashboardContext.Api("/moderation/unban-address"), guard.Admin(async (request, user) =>
        {
            string address = DashboardGuard.Read<AddressRequest>(request)?.Address?.Trim();
            if (string.IsNullOrEmpty(address)) return HttpResponse.Error("Missing address", 400);

            bool removed = await MainThread.Run(() => ServerModeration.UnbanAddress(address));
            if (removed) DashboardContext.Audit(user, $"unbanned address {address}");
            return HttpResponse.Json(new { ok = removed });
        }));

        router.Map("POST", DashboardContext.Api("/whitelist/enabled"), guard.Admin(async (request, user) =>
        {
            var body = DashboardGuard.Read<ToggleRequest>(request);
            if (body == null) return HttpResponse.Error("Bad request", 400);

            await MainThread.Run(() =>
            {
                config.WhitelistEnabled = body.Enabled;
                SaveWhitelist(config);
            });
            DashboardContext.Audit(user, body.Enabled ? "enabled the whitelist" : "disabled the whitelist");
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("POST", DashboardContext.Api("/whitelist/add"), guard.Admin(async (request, user) =>
        {
            string name = DashboardGuard.Read<NameRequest>(request)?.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 32) return HttpResponse.Error("Enter a name (up to 32 characters)", 400);

            await MainThread.Run(() =>
            {
                if (!config.Whitelist.Exists(entry => string.Equals(entry, name, StringComparison.OrdinalIgnoreCase))) config.Whitelist.Add(name);
                SaveWhitelist(config);
            });
            DashboardContext.Audit(user, $"added {name} to the whitelist");
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("POST", DashboardContext.Api("/whitelist/remove"), guard.Admin(async (request, user) =>
        {
            string name = DashboardGuard.Read<NameRequest>(request)?.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return HttpResponse.Error("Missing name", 400);

            await MainThread.Run(() =>
            {
                config.Whitelist.RemoveAll(entry => string.Equals(entry, name, StringComparison.OrdinalIgnoreCase));
                SaveWhitelist(config);
            });
            DashboardContext.Audit(user, $"removed {name} from the whitelist");
            return HttpResponse.Json(new { ok = true });
        }));
    }

    private static void SaveWhitelist(Dedicated.ServerConfig config)
    {
        config.Save();
        NetworkSession.Current?.ReloadAccessSettings();
    }
}
