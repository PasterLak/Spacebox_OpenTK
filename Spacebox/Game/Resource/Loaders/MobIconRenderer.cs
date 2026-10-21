using Engine;
using Engine.Graphics.Atlas;
using Engine.Graphics.Icons;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Mobs;

namespace Spacebox.Game.Resource;

public static class MobIconRenderer
{
    public const int Size = 64;

    public static void ApplyToSpawners()
    {
        if (Application.IsHeadless || GameAssets.AtlasItems == null) return;

        foreach (var item in GameAssets.Items.Values)
        {
            if (item is not MobSpawnerItem spawner || !GameAssets.Mobs.TryGetValue(spawner.MobId, out var mob)) continue;
            if (!TryRender(mob, out var pixels)) continue;

            string iconName = IconName(mob);
            GameAssets.AtlasItems.Add(iconName, pixels, true);
            spawner.Icon = GameAssets.AtlasItems.GetSprite(iconName);
        }
    }

    public static string IconName(MobDefinition mob) => "mob_icon_" + mob.Id;

    public static bool TryRender(MobDefinition mob, out Color4[,] pixels)
    {
        pixels = null;
        string modelPath = ResolvePath(mob.ModelPath);
        string texturePath = ResolvePath(mob.TexturePath);
        if (modelPath == null || texturePath == null || !ImagePixels.TryLoadTopDown(texturePath, out var texture)) return false;

        try
        {
            var (vertices, indices) = ObjLoader.Load(modelPath);
            if (vertices.Length == 0 || indices.Length < 3) return false;

            var triangles = new uint[indices.Length];
            for (int i = 0; i < indices.Length; i++) triangles[i] = (uint)indices[i];

            pixels = IsometricRasterizer.Render(vertices, triangles, Size, PixelIconSampler.FromTopDown(texture, true), true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.Warning($"[MobIconRenderer] Icon for mob '{mob.Id}' failed: {ex.Message}");
            return false;
        }
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (File.Exists(path)) return path;

        string fromBase = Path.Combine(AppContext.BaseDirectory, path);
        return File.Exists(fromBase) ? fromBase : null;
    }
}
