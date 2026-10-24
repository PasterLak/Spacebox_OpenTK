using Engine;
using Engine.Web;
using OpenTK.Mathematics;
using Spacebox.Network.Dashboard.Map;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class MapModule : IDashboardModule
{
    public const float DefaultRadius = 2000f;

    private readonly WorldMapSource _source = new WorldMapSource();
    private readonly MapSnapshotBuilder _builder = new MapSnapshotBuilder();

    public void Map(HttpRouter router, DashboardContext context)
    {
        router.Map("GET", DashboardContext.Api("/map"), context.Guard.Admin(async (request, _) =>
        {
            float radius = ParseRadius(request.QueryValue("radius"));
            string focus = request.QueryValue("player");

            var snapshot = await MainThread.Run(() => Build(radius, focus));
            return HttpResponse.Json(snapshot);
        }));
    }

    public static float ParseRadius(string text)
    {
        return float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var radius) && float.IsFinite(radius)
            ? radius
            : DefaultRadius;
    }

    private MapSnapshot Build(float radius, string focusPlayer)
    {
        var players = _source.CollectPlayers();
        Vector3? focus = null;
        if (!string.IsNullOrWhiteSpace(focusPlayer))
        {
            foreach (var player in players)
            {
                if (string.Equals(player.Name, focusPlayer, StringComparison.OrdinalIgnoreCase)) focus = player.Position;
            }
        }

        if (focus == null && players.Count == 0) focus = _source.DefaultCenter();

        return _builder.Build(players, _source.CollectEntities(), _source.CollectClaims(), radius, focus);
    }
}
