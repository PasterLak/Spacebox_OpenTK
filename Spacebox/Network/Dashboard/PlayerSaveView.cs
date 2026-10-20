using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spacebox.Network.Dashboard;

public sealed class PlayerSlotView
{
    public string Id { get; init; } = "";
    public string Name { get; set; } = "";
    public int Count { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
}

public sealed class PlayerSaveView
{
    public float Health { get; init; }
    public float Power { get; init; }
    public float Oxygen { get; init; }
    public float[] Position { get; init; } = new float[3];
    public List<PlayerSlotView> Inventory { get; init; } = new List<PlayerSlotView>();
    public List<PlayerSlotView> Panel { get; init; } = new List<PlayerSlotView>();
    public List<PlayerSlotView> Equipment { get; init; } = new List<PlayerSlotView>();
    public JsonNode Statistics { get; init; }

    public IEnumerable<PlayerSlotView> AllSlots => Inventory.Concat(Panel).Concat(Equipment);

    public static PlayerSaveView Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        JsonObject root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
        if (root == null) return null;

        return new PlayerSaveView
        {
            Health = Number(root, "Health"),
            Power = Number(root, "Power"),
            Oxygen = Number(root, "Oxygen"),
            Position = new[] { Number(root, "PositionX"), Number(root, "PositionY"), Number(root, "PositionZ") },
            Inventory = Slots(root, "InventorySlots"),
            Panel = Slots(root, "PanelSlots"),
            Equipment = Slots(root, "EquipmentSlots"),
            Statistics = root["Statistics"]?.DeepClone()
        };
    }

    private static float Number(JsonObject root, string key)
    {
        try
        {
            return root[key]?.GetValue<float>() ?? 0f;
        }
        catch (Exception)
        {
            return 0f;
        }
    }

    private static List<PlayerSlotView> Slots(JsonObject root, string key)
    {
        var result = new List<PlayerSlotView>();
        if (root[key] is not JsonArray slots) return result;

        foreach (var node in slots)
        {
            if (node is not JsonObject slot) continue;

            string id = slot["ItemID"]?.GetValue<string>() ?? "";
            int count = (int)Number(slot, "Count");
            if (string.IsNullOrEmpty(id) || count <= 0) continue;

            result.Add(new PlayerSlotView { Id = id, Count = count, X = (int)Number(slot, "SlotX"), Y = (int)Number(slot, "SlotY") });
        }

        return result;
    }
}
