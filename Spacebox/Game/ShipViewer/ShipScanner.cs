using Engine;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipScanResult
{
    public ShipScanResult(List<SavedShip> ships, int unreadableFiles)
    {
        Ships = ships;
        UnreadableFiles = unreadableFiles;
    }

    public List<SavedShip> Ships { get; }
    public int UnreadableFiles { get; }
}

public static class ShipScanner
{
    public static ShipScanResult Scan(string worldFolder, CancellationToken token = default)
    {
        var ships = new List<SavedShip>();
        int unreadable = 0;

        foreach (var file in WorldFileLayout.EntityFilePaths(worldFolder))
        {
            if (token.IsCancellationRequested) break;

            try
            {
                var entity = NbtFileEditor.Read(file);
                if (!ShipTagReader.IsPlayerBuilt(entity)) continue;

                var stats = ShipTagReader.Measure(entity);
                if (stats.IsEmpty) continue;

                ships.Add(new SavedShip(file, ShipTagReader.ReadName(entity, EntityFiles.NameOf(file)), ShipTagReader.ReadId(entity), stats));
            }
            catch (Exception ex)
            {
                unreadable++;
                Debug.Warning($"[ShipViewer] Skipped unreadable entity file {file}: {ex.Message}");
            }
        }

        ships.Sort((a, b) => b.Stats.Blocks.CompareTo(a.Stats.Blocks));
        return new ShipScanResult(ships, unreadable);
    }
}
