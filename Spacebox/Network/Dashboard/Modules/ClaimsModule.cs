using Engine;
using Engine.Web;
using Spacebox.Game.Protection;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class ClaimsModule : IDashboardModule
{
    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;
        var config = context.Config;

        router.Map("GET", DashboardContext.Api("/claims"), guard.Admin(async (_, _) => HttpResponse.Json(await MainThread.Run(() => (object)new
        {
            settings = config.Claims,
            loaded = ClaimRegistry.Loaded.Count,
            total = ClaimRegistry.Index.Count
        }))));

        router.Map("POST", DashboardContext.Api("/claims/settings"), guard.Admin(async (request, user) =>
        {
            var settings = Parse(request);
            if (settings == null) return HttpResponse.Error("Bad request", 400);

            bool corrected = settings.Validate();
            await MainThread.Run(() =>
            {
                config.Claims = settings;
                config.Save();
                ClaimNetwork.BroadcastSettings();
            });
            DashboardContext.Audit(user, "changed the claim settings");
            return HttpResponse.Json(new { ok = true, corrected });
        }));
    }

    public static ClaimSettings Parse(HttpRequest request) => DashboardGuard.Read<ClaimSettings>(request);
}
