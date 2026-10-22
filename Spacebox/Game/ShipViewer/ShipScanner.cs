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

    public string Label(int index)
    {
        var ship = Ships[index];
        if (ship.HasCustomName) return ship.Name;

        int number = 1;
        for (int i = 0; i < index; i++)
            if (!Ships[i].HasCustomName && Ships[i].HasShipParts == ship.HasShipParts) number++;
        return $"{(ship.HasShipParts ? "Ship" : "Base")} {number}";
    }
}

public static class ShipScanner
{
    public static ShipScanResult Scan(string worldFolder, CancellationToken token = default)
    {
        return Scan(worldFolder, null, token);
    }

    public static ShipScanResult Scan(string worldFolder, Func<string, bool> isShipPart, CancellationToken token = default)
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

                bool hasShipParts = isShipPart == null || ShipTagReader.ContainsBlock(entity, isShipPart);
                ships.Add(new SavedShip(file, ShipTagReader.ReadName(entity, EntityFiles.NameOf(file)), ShipTagReader.ReadId(entity), stats, hasShipParts));
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
