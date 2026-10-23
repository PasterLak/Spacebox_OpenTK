using Engine;
using SharpNBT;
using Spacebox.Game.Ships.Sharing;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipExportJob
{
    private const int MaxWaitFrames = 30;

    private readonly ShipFile _ship;
    private readonly string _picturePath;
    private int _framesLeft = MaxWaitFrames;

    public ShipExportJob(ShipFile ship, string path, string picturePath)
    {
        _ship = ship;
        Path = path;
        _picturePath = picturePath;
    }

    public string Path { get; }
    public bool IsDone { get; private set; }
    public string Error { get; private set; }

    public static ShipFile Create(CompoundTag entity, SavedWorld world, string name, string author, string description)
    {
        var stats = ShipTagReader.Measure(entity);
        var ship = new ShipFile
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Ship" : name.Trim(),
            Author = author?.Trim() ?? "",
            Description = description?.Trim() ?? "",
            GameVersion = Application.Version,
            Created = DateTime.UtcNow.ToString("O"),
            Blocks = stats.Blocks,
            Min = stats.Min,
            Max = stats.Max,
            Entity = entity
        };
        ship.Id = ShipFileNames.MakeId(ship.Name);
        if (!string.IsNullOrEmpty(world?.ModId)) ship.GameSets.Add(world.ModId);
        if (world?.Info?.Extensions != null) ship.GameSets.AddRange(world.Info.Extensions);
        return ship;
    }

    public void Update()
    {
        if (IsDone) return;
        if (!File.Exists(_picturePath) && --_framesLeft > 0) return;

        try
        {
            if (File.Exists(_picturePath))
            {
                _ship.Picture = File.ReadAllBytes(_picturePath);
                File.Delete(_picturePath);
            }
            ShipFileCodec.Write(Path, _ship);
            Debug.Success($"[Ships] Exported '{_ship.Name}' to {Path}");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            Debug.Error($"[Ships] Export failed: {ex.Message}");
        }
        IsDone = true;
    }
}
