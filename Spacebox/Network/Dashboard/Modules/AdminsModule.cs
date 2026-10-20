using Engine;
using Engine.Web;
using Spacebox.Network.Admin;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class AdminsModule : IDashboardModule
{
    private sealed class ToggleRequest
    {
        public bool Enabled { get; set; }
    }

    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;
        var config = context.Config;

        router.Map("GET", DashboardContext.Api("/admins"), guard.Admin(async (_, _) => HttpResponse.Json(await MainThread.Run(() => (object)new
        {
            admins = config.Admins.ToArray(),
            showAdmins = config.ShowAdmins,
            rightsActive = ServerAdmins.RightsActive(config)
        }))));

        router.Map("POST", DashboardContext.Api("/admins/add"), guard.Admin(async (request, user) =>
        {
            string name = DashboardGuard.Read<NameRequest>(request)?.Name;
            string error = await MainThread.Run(() =>
            {
                if (!ServerAdmins.TryAdd(config.Admins, name, out string reason)) return reason;
                Save(config);
                return null;
            });
            if (error != null) return HttpResponse.Error(error, 400);

            DashboardContext.Audit(user, $"made {name.Trim()} an admin");
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("POST", DashboardContext.Api("/admins/remove"), guard.Admin(async (request, user) =>
        {
            string name = DashboardGuard.Read<NameRequest>(request)?.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return HttpResponse.Error("Missing name", 400);

            bool removed = await MainThread.Run(() =>
            {
                bool changed = ServerAdmins.Remove(config.Admins, name);
                if (changed) Save(config);
                return changed;
            });
            if (removed) DashboardContext.Audit(user, $"removed admin rights from {name}");
            return HttpResponse.Json(new { ok = removed });
        }));

        router.Map("POST", DashboardContext.Api("/admins/show"), guard.Admin(async (request, user) =>
        {
            var body = DashboardGuard.Read<ToggleRequest>(request);
            if (body == null) return HttpResponse.Error("Bad request", 400);

            await MainThread.Run(() =>
            {
                config.ShowAdmins = body.Enabled;
                Save(config);
            });
            DashboardContext.Audit(user, body.Enabled ? "shows admins" : "hides admins");
            return HttpResponse.Json(new { ok = true });
        }));
    }

    private static void Save(Dedicated.ServerConfig config)
    {
        config.Save();
        PlayerListNetwork.MarkDirty();
        ProtectionNetwork.BroadcastState();
    }
}
