using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player.Respawn;

public static class WorldSpawn
{
    public static bool TryRead(float[] stored, out Vector3 position)
    {
        if (stored is { Length: 3 } && float.IsFinite(stored[0]) && float.IsFinite(stored[1]) && float.IsFinite(stored[2]))
        {
            position = new Vector3(stored[0], stored[1], stored[2]);
            return true;
        }

        position = Vector3.Zero;
        return false;
    }

    public static float[] ToStored(Vector3 position) => new[] { position.X, position.Y, position.Z };

    public static bool TryGet(out Vector3 position) => TryRead(World.WorldData?.Info?.SpawnPoint, out position);

    public static Vector3 For(LocalAstronaut player) => TryGet(out var position) ? position : player.SpawnPosition;

    public static void Apply(Vector3? position)
    {
        var info = World.WorldData?.Info;
        if (info == null) return;

        info.SpawnPoint = position.HasValue ? ToStored(position.Value) : null;
        if (info.IsLocalWorld) GUI.WorldInfoSaver.Save(info);
    }
}
