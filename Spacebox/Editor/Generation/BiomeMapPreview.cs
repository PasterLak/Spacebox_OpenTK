using Engine;
using Engine.Multithreading;
using Engine.Utils;
using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Editor.Generation;

public sealed class BiomeMapPreview : IDisposable
{
    private const int SliceBand = 3;

    private volatile BuildResult _pending;
    private BuildResult _result;
    private Texture2D _texture;
    private int _textureSlice = -1;
    private bool _building;

    public bool IsBuilding => _building;
    public bool HasMap => _result != null;
    public int AsteroidCount => _result?.Asteroids.Length ?? 0;

    public void Build(string seedText, Vector3i sectorIndex)
    {
        var generator = World.WorldGenerator;
        if (generator == null || _building) return;

        _building = true;
        int worldSeed = SeedHelper.ComputeSeed(seedText ?? "");

        WorkerPoolManager.Enqueue(_ =>
        {
            try
            {
                var map = new BiomesMap(sectorIndex, new BiomeGenerator(worldSeed, generator));
                var origin = SpaceMath.Sector.GetSectorPosition(sectorIndex);
                var points = SpaceMath.Sector.GenerateAsteroidPositions(SeedHelper.GetSectorId(worldSeed, sectorIndex), origin,
                    generator.MinAsteroidsInSector, generator.MaxAsteroidsInSector, generator.RejectionSamples, generator.MinDistanceBetweenAsteroids, true);

                var asteroids = new List<Vector3>(points.Length);
                foreach (var point in points)
                {
                    var local = BiomeMapImage.ToSectorLocal(point, origin, Sector.SizeBlocks);
                    var biome = map.GetFromSectorLocalCoord(local);
                    if (biome != null && biome.AsteroidChances.Count > 0) asteroids.Add(local);
                }

                _pending = new BuildResult(map, asteroids.ToArray());
            }
            catch (Exception ex)
            {
                Engine.Debug.Error($"[Editor] Biome map failed: {ex.Message}");
                _pending = new BuildResult(null, Array.Empty<Vector3>());
            }
        }, WorkerPoolManager.Priority.Medium);
    }

    public void Draw(System.Numerics.Vector2 size, int slice)
    {
        AcceptResult();
        if (_result?.Map == null)
        {
            ImGui.TextDisabled(_building ? "Building biome map..." : "Press Build to generate the biome map of a sector.");
            return;
        }

        UpdateTexture(slice);
        float side = MathF.Min(size.X, size.Y);
        var imageSize = new System.Numerics.Vector2(side, side);
        var origin = ImGui.GetCursorScreenPos();
        ImGui.Image(_texture.Handle, imageSize);

        var drawList = ImGui.GetWindowDrawList();
        foreach (var asteroid in _result.Asteroids)
        {
            bool inSlice = BiomeMapImage.IsInSlice(asteroid.Y, slice, BiomesMap.Resolution, Sector.SizeBlocks, SliceBand);
            var p = BiomeMapImage.ToImage(asteroid, Sector.SizeBlocks, new Vector2(side, side));
            drawList.AddCircleFilled(origin + new System.Numerics.Vector2(p.X, p.Y), inSlice ? 3f : 1.5f, inSlice ? 0xFFFFFFFF : 0x60FFFFFF);
        }

        if (!ImGui.IsItemHovered()) return;

        var mouse = ImGui.GetMousePos() - origin;
        int x = Math.Clamp((int)(mouse.X / side * BiomesMap.Resolution), 0, BiomesMap.Resolution - 1);
        int z = Math.Clamp((int)(mouse.Y / side * BiomesMap.Resolution), 0, BiomesMap.Resolution - 1);
        var biome = _result.Map.GetValue((byte)x, (byte)slice, (byte)z);
        ImGui.SetTooltip(biome == null ? "No biome" : $"{biome.Name} ({biome.IdString})\nblocks x {x * BiomesMap.SmallestPoint}, z {z * BiomesMap.SmallestPoint}");
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _texture = null;
    }

    private void AcceptResult()
    {
        var pending = _pending;
        if (pending == null) return;

        _pending = null;
        _result = pending;
        _building = false;
        _textureSlice = -1;
    }

    private void UpdateTexture(int slice)
    {
        if (_texture != null && _textureSlice == slice) return;

        int resolution = BiomesMap.Resolution;
        _texture ??= Texture2D.CreateTexture(resolution, resolution, Color4.Black, FilterMode.Nearest);

        var map = _result.Map;
        var pixels = BiomeMapImage.BuildSlice(resolution, resolution, (x, z) =>
        {
            var biome = map.GetValue((byte)x, (byte)slice, (byte)z);
            if (biome == null) return Color4.Black;
            var c = biome.DebugColor;
            return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, 1f);
        });

        _texture.SetPixelsData(pixels);
        _texture.UpdateTexture();
        _textureSlice = slice;
    }

    private sealed record BuildResult(BiomesMap Map, Vector3[] Asteroids);
}
