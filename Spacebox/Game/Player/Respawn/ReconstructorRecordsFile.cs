using System.Text.Json;
using Engine;
using Engine.Utils;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Respawn;

public static class ReconstructorRecordsFile
{
    public const string FileName = "reconstructors.json";

    public sealed class Entry
    {
        public long EntityId { get; set; }
        public int[] Cell { get; set; } = new int[3];
        public int[] Sector { get; set; } = new int[3];
        public float[] Position { get; set; } = new float[3];
        public int Dna { get; set; }
        public bool Powered { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    public static string PathOf(string worldFolder) => Path.Combine(worldFolder, FileName);

    public static void Write(string worldFolder, IEnumerable<ReconstructorRecord> records)
    {
        var entries = new List<Entry>();
        foreach (var record in records) entries.Add(ToEntry(record));

        string path = PathOf(worldFolder);
        if (entries.Count == 0)
        {
            if (File.Exists(path)) File.Delete(path);
            return;
        }

        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(entries, Options));
    }

    public static List<ReconstructorRecord> Read(string worldFolder)
    {
        var records = new List<ReconstructorRecord>();
        string path = PathOf(worldFolder);
        if (!File.Exists(path)) return records;

        try
        {
            var entries = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(path));
            if (entries == null) return records;

            foreach (var entry in entries)
            {
                if (TryFromEntry(entry, out var record)) records.Add(record);
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[Reconstructors] Failed to read {FileName}: {ex.Message}");
        }

        return records;
    }

    private static Entry ToEntry(ReconstructorRecord record)
    {
        return new Entry
        {
            EntityId = record.Key.EntityId,
            Cell = new[] { record.Key.Cell.X, record.Key.Cell.Y, record.Key.Cell.Z },
            Sector = new[] { record.Sector.X, record.Sector.Y, record.Sector.Z },
            Position = new[] { record.Local.X, record.Local.Y, record.Local.Z },
            Dna = record.DnaCount,
            Powered = record.IsPowered
        };
    }

    private static bool TryFromEntry(Entry entry, out ReconstructorRecord record)
    {
        record = default;
        if (entry?.Cell is not { Length: 3 } cell || entry.Sector is not { Length: 3 } sector || entry.Position is not { Length: 3 } position) return false;

        var key = new ReconstructorKey(entry.EntityId, new Vector3i(cell[0], cell[1], cell[2]));
        record = new ReconstructorRecord(key, new Vector3i(sector[0], sector[1], sector[2]), new Vector3(position[0], position[1], position[2]), Math.Max(0, entry.Dna), entry.Powered);
        return true;
    }
}
