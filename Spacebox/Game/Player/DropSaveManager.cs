using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spacebox.Game.Player;

public class DropSaveData
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    [JsonIgnore]
    public Vector3 Position
    {
        get => new Vector3(X, Y, Z);
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }
}

public static class DropSaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
       
    };

    public static List<DropSaveData> SerializeDrops(IEnumerable<Drop> drops)
    {
        var dropData = new List<DropSaveData>();

        foreach (var drop in drops)
        {
            if (drop.IsActive && drop.Info.item != null)
            {
                dropData.Add(new DropSaveData
                {
                    ItemId = drop.Info.item.Id_string,
                    Quantity = drop.Info.quantity,
                    Position = drop.Position
                });
            }
        }

        return dropData;
    }

    public static void SaveToFile(List<DropSaveData> dropData, string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(dropData, JsonOptions);
            File.WriteAllText(filePath, json);
            Debug.Log($"[DropSaveManager] Saved {dropData.Count} drops to {filePath}");
        }
        catch (Exception ex)
        {
            Debug.Error($"[DropSaveManager] Failed to save drops: {ex.Message}");
        }
    }

    public static List<DropSaveData> LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            // Debug.Log($"[DropSaveManager] Drop save file not found: {filePath}");
            return new List<DropSaveData>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var dropData = JsonSerializer.Deserialize<List<DropSaveData>>(json, JsonOptions);

            if (dropData == null)
            {
                Debug.Error("[DropSaveManager] Failed to deserialize drop data");
                return new List<DropSaveData>();
            }

            Debug.Log($"[DropSaveManager] Loaded {dropData.Count} drops from {filePath}");
            return dropData;
        }
        catch (Exception ex)
        {
            Debug.Error($"[DropSaveManager] Failed to load drops: {ex.Message}");
            return new List<DropSaveData>();
        }
    }
}

