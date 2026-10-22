using Engine;
using Engine.Multithreading;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Editor.Generation;

public sealed class BiomeMapPreview : IDisposable
{
    public const int SliceBand = 3;

    private volatile BuildResult _pending;
    private BuildResult _result;
    private Texture2D _texture;
    private int _textureSlice = -1;
    private bool _building;

    public bool IsBuilding => _building;
    public bool HasMap => _result != null;
    public int AsteroidCount => _result?.Asteroids.Length ?? 0;

    public void Build(string seedText, Vector3i sectorIndex, int generatorVersion)
    {
        var generator = World.WorldGenerator;
        if (generator == null || _building) return;

        _building = true;
        int worldSeed = SeedHelper.ComputeSeed(seedText ?? "");

        WorkerPoolManager.Enqueue(_ =>
        {
            try
            {
                var map = new BiomesMap(sectorIndex, new BiomeGenerator(worldSeed, generator), generatorVersion);
                var origin = SpaceMath.Sector.GetSectorPosition(sectorIndex);
                long sectorSeed = SeedHelper.GetSectorId(worldSeed, sectorIndex);
                var asteroids = GeneratorVersions.UsesRules(generatorVersion)
                    ? PlannedAsteroids(generator, sectorSeed, origin, map, generatorVersion)
                    : LegacyAsteroids(generator, sectorSeed, origin, map);

                _pending = new BuildResult(map, asteroids.ToArray(), origin, generator.Biomes);
            }
            catch (Exception ex)
            {
                Engine.Debug.Error($"[Editor] Biome map failed: {ex.Message}");
                _pending = new BuildResult(null, Array.Empty<Vector3>(), Vector3.Zero, Array.Empty<Biome>());
            }
        }, WorkerPoolManager.Priority.Medium);
    }

    private static List<Vector3> PlannedAsteroids(WorldGenerator generator, long sectorSeed, Vector3 origin, BiomesMap map, int generatorVersion)
    {
        var planned = SectorAsteroidPlanner.Plan(generator, sectorSeed, origin, map.GetFromSectorLocalCoord, point => SeedHelper.GetAsteroidId(sectorSeed, point) & 0x7FFFFFFFFFFFFFFF, generatorVersion);
        var asteroids = new List<Vector3>(planned.Count);
        foreach (var data in planned) asteroids.Add(data.positionInSector);
        return asteroids;
    }

    private static List<Vector3> LegacyAsteroids(WorldGenerator generator, long sectorSeed, Vector3 origin, BiomesMap map)
    {
        var points = SpaceMath.Sector.GenerateAsteroidPositions(sectorSeed, origin,
            generator.MinAsteroidsInSector, generator.MaxAsteroidsInSector, generator.RejectionSamples, generator.MinDistanceBetweenAsteroids, true);

        var asteroids = new List<Vector3>(points.Length);
        foreach (var point in points)
        {
            var local = BiomeMapImage.ToSectorLocal(point, origin, Sector.SizeBlocks);
            var biome = map.GetFromSectorLocalCoord(local);
            if (biome != null && biome.AsteroidChances.Count > 0) asteroids.Add(local);
        }
        return asteroids;
    }

    public Vector3[] Asteroids => _result?.Asteroids ?? Array.Empty<Vector3>();
    public Vector3 Origin => _result?.Origin ?? Vector3.Zero;
    public Biome[] Biomes => _result?.Biomes ?? Array.Empty<Biome>();

    public void Update()
    {
        AcceptResult();
    }

    public Texture2D GetSliceTexture(int slice)
    {
        if (_result?.Map == null) return null;
        UpdateTexture(slice);
        return _texture;
    }

    public Biome BiomeAt(int x, int slice, int z)
    {
        if (_result?.Map == null) return null;
        int last = BiomesMap.Resolution - 1;
        return _result.Map.GetValue((byte)Math.Clamp(x, 0, last), (byte)Math.Clamp(slice, 0, last), (byte)Math.Clamp(z, 0, last));
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

    private sealed record BuildResult(BiomesMap Map, Vector3[] Asteroids, Vector3 Origin, Biome[] Biomes);
}
