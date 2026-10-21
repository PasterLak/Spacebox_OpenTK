using OpenTK.Mathematics;

namespace Spacebox.Network.Dashboard.Map;

public sealed class MapSnapshotBuilder
{
    public const float MinRadius = 100f;
    public const float MaxRadius = 8192f;
    public const int MaxEntities = 20000;

    public MapSnapshot Build(
        IReadOnlyList<MapPlayerRecord> players,
        IReadOnlyList<MapEntityRecord> entities,
        IReadOnlyDictionary<long, MapClaimRecord> claims,
        float radius,
        Vector3? focus = null)
    {
        radius = Math.Clamp(radius, MinRadius, MaxRadius);
        Vector3 center = focus ?? AverageOf(players);
        float radiusSquared = radius * radius;

        var owners = new List<string>();
        var ownerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<float[]>();
        var ids = new List<string>();
        int truncated = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!IsVisible(entity.Position, players, center, focus.HasValue, radiusSquared)) continue;
            if (rows.Count >= MaxEntities)
            {
                truncated++;
                continue;
            }

            int flags = FlagsOf(entity);
            int owner = -1;
            if (claims.TryGetValue(entity.Id, out var claim))
            {
                flags |= MapSnapshot.FlagClaimed;
                if (claim.IsActive) flags |= MapSnapshot.FlagClaimActive;
                owner = IndexOf(claim.Owner, owners, ownerIndex);
            }

            var local = entity.Position - center;
            rows.Add(new[] { Round(local.X), Round(local.Y), Round(local.Z), Round(entity.Radius), flags, owner });
            ids.Add(entity.Id.ToString());
        }

        var playerViews = new List<MapPlayerView>(players.Count);
        for (int i = 0; i < players.Count; i++)
        {
            var local = players[i].Position - center;
            playerViews.Add(new MapPlayerView
            {
                Name = players[i].Name,
                Position = new[] { Round(local.X), Round(local.Y), Round(local.Z) },
                Alive = players[i].IsAlive
            });
        }

        return new MapSnapshot
        {
            Center = new double[] { center.X, center.Y, center.Z },
            Radius = radius,
            Players = playerViews,
            Entities = rows,
            Ids = ids,
            Owners = owners,
            Truncated = truncated
        };
    }

    public static int FlagsOf(MapEntityRecord entity)
    {
        int flags = (int)entity.Persistence << MapSnapshot.PersistenceShift;
        if (entity.IsShip) flags |= MapSnapshot.FlagShip;
        if (entity.IsLoaded) flags |= MapSnapshot.FlagLoaded;
        return flags;
    }

    public static bool TryParseClaimEntity(string key, out long entityId)
    {
        entityId = 0;
        if (string.IsNullOrEmpty(key)) return false;
        int separator = key.IndexOf(':');
        return long.TryParse(separator < 0 ? key : key.Substring(0, separator), out entityId);
    }

    private static bool IsVisible(Vector3 position, IReadOnlyList<MapPlayerRecord> players, Vector3 center, bool hasFocus, float radiusSquared)
    {
        if (hasFocus || players.Count == 0) return Vector3.DistanceSquared(position, center) <= radiusSquared;

        for (int i = 0; i < players.Count; i++)
        {
            if (Vector3.DistanceSquared(position, players[i].Position) <= radiusSquared) return true;
        }
        return false;
    }

    private static Vector3 AverageOf(IReadOnlyList<MapPlayerRecord> players)
    {
        if (players.Count == 0) return Vector3.Zero;

        Vector3 sum = Vector3.Zero;
        for (int i = 0; i < players.Count; i++) sum += players[i].Position;
        return sum / players.Count;
    }

    private static int IndexOf(string owner, List<string> owners, Dictionary<string, int> index)
    {
        owner ??= "";
        if (index.TryGetValue(owner, out int value)) return value;
        value = owners.Count;
        owners.Add(owner);
        index[owner] = value;
        return value;
    }

    private static float Round(float value) => MathF.Round(value, 1);
}
