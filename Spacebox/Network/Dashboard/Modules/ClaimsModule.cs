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
            total = ClaimRegistry.Index.Count,
            items = Items()
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

    private static List<object> Items()
    {
        var items = new List<(string Id, string Name)>();
        foreach (var pair in Spacebox.Game.GameAssets.ItemsStr)
        {
            if (pair.Value.Id != 0) items.Add((pair.Key, pair.Value.Name));
        }
        items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        var result = new List<object>(items.Count);
        foreach (var item in items) result.Add(new { id = item.Id, name = item.Name });
        return result;
    }
}
