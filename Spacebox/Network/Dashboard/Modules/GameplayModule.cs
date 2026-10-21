using Engine;
using Engine.Web;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class GameplayModule : IDashboardModule
{
    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;

        router.Map("GET", DashboardContext.Api("/gameplay"), guard.Admin(async (_, _) => HttpResponse.Json(await MainThread.Run(() => (object)new
        {
            rules = ServerRulesService.Snapshot()
        }))));

        router.Map("POST", DashboardContext.Api("/gameplay"), guard.Admin(async (request, user) =>
        {
            var values = DashboardGuard.Read<Dictionary<string, bool>>(request);
            if (values == null) return HttpResponse.Error("Bad request", 400);

            await MainThread.Run(() => ServerRulesService.Apply(values));
            DashboardContext.Audit(user, "changed the gameplay rules");
            return HttpResponse.Json(new { ok = true });
        }));
    }
}
