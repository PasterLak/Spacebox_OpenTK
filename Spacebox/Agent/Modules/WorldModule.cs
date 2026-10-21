using Engine.Web;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Agent.Modules;

public sealed class WorldModule : IAgentModule
{
    private const int MaxRadius = 32;

    public void Map(AgentRouter router)
    {
        router.Get("/block", "Block at a world point: ?x=&y=&z=", request =>
        {
            var position = AgentQuery.Vector(request);
            var grid = new MobNavigationGrid();
            grid.Bind(position);
            var cell = grid.WorldToCell(position);
            var block = grid.GetBlock(cell);
            return AgentJson.Response(new
            {
                entityId = grid.Anchor?.EntityID,
                entityCell = grid.Anchor != null ? AgentGame.Vec(cell) : null,
                center = AgentGame.Vec(grid.CellToWorld(cell)),
                block = AgentGame.DescribeBlock(block)
            });
        });

        router.Get("/blocks", "Non-air blocks near ?x=&y=&z= (default: player) within ?radius=8 (max 32); filter ?id= (part of full id or name), ?limit=50", Scan);

        router.Get("/entities", "Asteroids and ships in the current sector, nearest first: ?radius=2000&limit=30", request =>
        {
            var player = AgentGame.RequirePlayer();
            var sector = World.CurrentSector ?? throw new AgentRequestException("No sector is loaded", 409);
            float radius = AgentQuery.Float(request, "radius", 2000f);
            int limit = AgentQuery.Int(request, "limit", 30);

            var rows = new List<(float distance, object row)>();
            foreach (var entity in sector.Entities)
            {
                var bounds = entity.ToWorldBounds(entity.GeometryBoundingBox);
                float distance = DistanceToBox(player.Position, bounds.Min, bounds.Max);
                if (distance > radius) continue;

                rows.Add((distance, new
                {
                    id = entity.EntityID,
                    type = entity.GetType().Name,
                    name = entity.Name,
                    distance = AgentGame.Round(distance),
                    center = AgentGame.Vec(bounds.Center),
                    min = AgentGame.Vec(bounds.Min),
                    max = AgentGame.Vec(bounds.Max),
                    chunks = entity.Chunks.Count,
                    ship = entity.HasShipControl,
                    rotated = entity.HasRotation
                }));
            }

            rows.Sort((a, b) => a.distance.CompareTo(b.distance));
            return AgentJson.Response(rows.Take(limit).Select(r => r.row));
        });
    }

    private static HttpResponse Scan(HttpRequest request)
    {
        var center = AgentQuery.HasVector(request) ? AgentQuery.Vector(request) : AgentGame.RequirePlayer().Position;
        int radius = Math.Clamp(AgentQuery.Int(request, "radius", 8), 1, MaxRadius);
        int limit = AgentQuery.Int(request, "limit", 50);
        string filter = AgentQuery.Text(request, "id");

        var grid = new MobNavigationGrid();
        grid.Bind(center);
        if (grid.Anchor == null)
        {
            var sector = World.SectorAt(center);
            if (sector != null && sector.TryGetNearestEntity(center, out var nearest) && nearest != null) grid.Bind(nearest);
        }
        if (grid.Anchor == null) return AgentJson.Response(new { entityId = (long?)null, total = 0, counts = new Dictionary<string, int>(), blocks = Array.Empty<object>() });

        var origin = grid.WorldToCell(center);
        var counts = new Dictionary<string, int>();
        var found = new List<(float distance, object row)>();
        int radiusSquared = radius * radius;

        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        for (int z = -radius; z <= radius; z++)
        {
            if (x * x + y * y + z * z > radiusSquared) continue;

            var cell = origin + new Vector3i(x, y, z);
            var block = grid.GetBlock(cell);
            if (block == null || block.IsAir) continue;

            var data = GameAssets.GetBlockDataById(block.Id);
            string fullId = data?.Id_string ?? block.Id.ToString();
            if (filter != null && !Matches(fullId, data?.Name, filter)) continue;

            counts[fullId] = counts.TryGetValue(fullId, out int count) ? count + 1 : 1;
            var world = grid.CellToWorld(cell);
            found.Add((Vector3.Distance(center, world), new { center = AgentGame.Vec(world), entityCell = AgentGame.Vec(cell), id = fullId, name = data?.Name }));
        }

        found.Sort((a, b) => a.distance.CompareTo(b.distance));
        return AgentJson.Response(new
        {
            entityId = grid.Anchor.EntityID,
            total = found.Count,
            counts = counts.OrderByDescending(c => c.Value).ToDictionary(c => c.Key, c => c.Value),
            blocks = found.Take(limit).Select(f => f.row)
        });
    }

    private static bool Matches(string fullId, string name, string filter)
    {
        return fullId.Contains(filter, StringComparison.OrdinalIgnoreCase) || (name != null && name.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    private static float DistanceToBox(Vector3 point, Vector3 min, Vector3 max)
    {
        var closest = Vector3.Clamp(point, min, max);
        return Vector3.Distance(point, closest);
    }
}
