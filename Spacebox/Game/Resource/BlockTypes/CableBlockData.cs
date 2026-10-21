using OpenTK.Mathematics;
using Spacebox.Game.Generation.Wires;

namespace Spacebox.Game.Resource;

public class CableBlockData : BlockData
{
    private const int TextureCount = 4;
    private const int TurnCount = 4;

    private readonly Vector2[][][] _active = new Vector2[TextureCount][][];
    private readonly Vector2[][][] _inactive = new Vector2[TextureCount][][];

    public bool HasWireLook { get; }

    public CableBlockData(BlockData baseBlock, CableJSON settings) : base(baseBlock)
    {
        settings ??= new CableJSON();
        HasWireLook = !string.IsNullOrWhiteSpace(settings.Straight);
        if (!HasWireLook) return;

        Load(WireTexture.Port, settings.Port, settings.PortOff);
        Load(WireTexture.Straight, settings.Straight, settings.StraightOff);
        Load(WireTexture.Angle, settings.Angle, settings.AngleOff);
        Load(WireTexture.Hub, settings.Hub, settings.HubOff);
    }

    public Vector2[] GetWireUVs(WireFace face, bool active)
    {
        var set = active ? _active : _inactive;
        return set[(int)face.Texture]?[face.QuarterTurns & (TurnCount - 1)];
    }

    public static Vector2[] Rotate(Vector2[] uvs, int quarterTurns)
    {
        var rotated = new Vector2[4];
        for (int i = 0; i < 4; i++)
            rotated[i] = uvs[(i + quarterTurns) & 3];
        return rotated;
    }

    private void Load(WireTexture texture, string active, string inactive)
    {
        _active[(int)texture] = Turns(Resolve(active));
        _inactive[(int)texture] = Turns(Resolve(string.IsNullOrWhiteSpace(inactive) ? active : inactive));
    }

    private static Vector2[] Resolve(string textureName)
    {
        if (string.IsNullOrWhiteSpace(textureName)) return null;

        var atlas = GameAssets.AtlasBlocks;
        if (atlas == null) return null;

        var region = atlas.GetRegion(textureName.Trim().ToLowerInvariant());
        if (region == null) Engine.Debug.Error($"[CableBlockData] Texture <{textureName}> was not found");
        return region?.Uvs;
    }

    private static Vector2[][] Turns(Vector2[] uvs)
    {
        if (uvs == null) return null;

        var turns = new Vector2[TurnCount][];
        for (int i = 0; i < TurnCount; i++)
            turns[i] = Rotate(uvs, i);
        return turns;
    }
}
