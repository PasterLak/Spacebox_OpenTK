using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Tutorial;

namespace Spacebox.Tests.Tutorial;

internal sealed class FakeItems : ITutorialItemResolver
{
    private readonly Dictionary<string, short> _ids = new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase)
    {
        { "aluminium_ore", 10 },
        { "cobaltite_rock", 11 },
        { "furnace", 20 },
        { "crafting_table", 21 },
        { "cobalt_drill", 30 },
        { "power", 40 },
        { "helmet", 50 }
    };

    public bool TryResolve(string id, out short itemId)
    {
        itemId = 0;
        return !string.IsNullOrWhiteSpace(id) && _ids.TryGetValue(id, out itemId);
    }
}

internal sealed class FakeContext : ITutorialContext
{
    public GameMode GameMode { get; set; } = GameMode.Survival;
    public Vector3 ViewDirection { get; set; } = -Vector3.UnitZ;
    public short SelectedItemId { get; set; }
    public bool IsMagnetEnabled { get; set; }
    public bool IsFlashlightOn { get; set; }
    public bool IsPiloting { get; set; }
    public bool IsPilotingFlight { get; set; }
    public bool IsUsingShipCamera { get; set; }
    public bool IsInGravity { get; set; }
    public bool HasAnyEquipment => Equipped.Count > 0;

    public Dictionary<short, int> Items { get; } = new Dictionary<short, int>();
    public HashSet<short> Equipped { get; } = new HashSet<short>();
    public HashSet<string> OpenWindows { get; } = new HashSet<string>();
    public HashSet<string> Pressed { get; } = new HashSet<string>();
    public HashSet<string> Held { get; } = new HashSet<string>();
    public Dictionary<string, double> Stats { get; } = new Dictionary<string, double>();

    public int CountItem(short itemId) => Items.TryGetValue(itemId, out var count) ? count : 0;
    public bool IsEquipped(short itemId) => Equipped.Contains(itemId);
    public bool IsSlotEquipped(EquipmentSlotType slot) => slot == EquipmentSlotType.Helmet && Equipped.Contains(50);
    public bool IsWindowOpen(string window) => OpenWindows.Contains(window);
    public bool IsActionPressed(string action) => Pressed.Contains(action);
    public bool IsActionHeld(string action) => Held.Contains(action);
    public double GetStat(string stat) => Stats.TryGetValue(stat, out var value) ? value : 0;
}

internal static class TutorialTestData
{
    public static TutorialDefinition Parse(string json, List<string> warnings = null)
    {
        return new TutorialParser(new FakeItems(), warnings ?? new List<string>()).Parse(json, "test.json");
    }

    public const string TwoChapters = """
    {
      "Id": "basics",
      "Chapters": [
        {
          "Id": "first",
          "Title": "First",
          "Reward": [{ "Item": "power", "Count": 2 }],
          "Steps": [
            { "Id": "fly", "Title": "Fly", "Tasks": [ { "Type": "press", "Action": "forward", "Count": 2 } ], "Reward": [{ "Item": "power", "Count": 1 }] },
            { "Id": "ore", "Title": "Ore", "Tasks": [ { "Type": "have_item", "Item": "aluminium_ore", "Count": 5 }, { "Type": "craft", "Item": "cobalt_drill", "Optional": true } ] }
          ]
        },
        {
          "Id": "second",
          "Title": "Second",
          "Steps": [
            { "Id": "place", "Tasks": [ { "Type": "place_block", "Block": "furnace" } ] },
            { "Id": "creative_only", "RequiresGameMode": "Creative", "Tasks": [ { "Type": "wait", "Seconds": 1 } ] }
          ]
        }
      ]
    }
    """;
}
