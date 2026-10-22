using System.Text.Json;
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
            rules = ServerRulesService.Describe()
        }))));

        router.Map("POST", DashboardContext.Api("/gameplay"), guard.Admin(async (request, user) =>
        {
            var values = DashboardGuard.Read<Dictionary<string, JsonElement>>(request);
            if (values == null) return HttpResponse.Error("Bad request", 400);

            var texts = new Dictionary<string, string>();
            foreach (var (name, value) in values) texts[name] = ToText(value);

            int result = await MainThread.Run(() => ServerRulesService.Apply(texts));
            if (result < 0) return HttpResponse.Error("Invalid value", 400);

            DashboardContext.Audit(user, "changed the gameplay rules");
            return HttpResponse.Json(new { ok = true });
        }));
    }

    private static string ToText(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => "on",
            JsonValueKind.False => "off",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String => value.GetString(),
            _ => ""
        };
    }
}
