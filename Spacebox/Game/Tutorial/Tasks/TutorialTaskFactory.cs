using Spacebox.Game.Player;

namespace Spacebox.Game.Tutorial.Tasks;

public static class TutorialTaskFactory
{
    private static readonly Dictionary<string, Func<TutorialTaskDefinition, ITutorialTask>> _types =
        new Dictionary<string, Func<TutorialTaskDefinition, ITutorialTask>>(StringComparer.OrdinalIgnoreCase)
        {
            { "press", d => new PressTask(d) },
            { "hold", d => new HoldTask(d) },
            { "wait", d => new WaitTask(d) },
            { "look", d => new LookTask(d) },
            { "move", d => new StatDeltaTask(d, nameof(PlayerStatistics.DistanceTraveled), d.Data.Distance) },
            { "ship_travel", d => new StatDeltaTask(d, nameof(PlayerStatistics.DistanceInShips), d.Data.Distance) },
            { "stat", d => new StatDeltaTask(d, d.Data.Stat, d.Data.Delta) },
            { "place_block", d => new ActivityCountTask(d, PlayerActivityKind.BlockPlaced) },
            { "destroy_block", d => new ActivityCountTask(d, PlayerActivityKind.BlockDestroyed) },
            { "pickup", d => new ActivityCountTask(d, PlayerActivityKind.ItemPickedUp) },
            { "craft", d => new ActivityCountTask(d, PlayerActivityKind.ItemCrafted) },
            { "process", d => new ActivityCountTask(d, PlayerActivityKind.ItemProcessed, d.Data.Machine) },
            { "use_item", d => new ActivityCountTask(d, PlayerActivityKind.ItemUsed) },
            { "use_block", d => new ActivityCountTask(d, PlayerActivityKind.BlockUsed) },
            { "bind_ship_part", d => new ActivityCountTask(d, PlayerActivityKind.ShipPartBound) },
            { "create_waypoint", d => new ActivityCountTask(d, PlayerActivityKind.WaypointCreated) },
            { "have_item", d => new HaveItemTask(d) },
            { "equip", d => new ConditionTask(d, CreateEquipCondition(d)) },
            { "open_ui", d => new ConditionTask(d, c => c.IsWindowOpen(d.Data.Window)) },
            { "select_hotbar", d => new ConditionTask(d, c => c.SelectedItemId == d.ItemId) },
            { "toggle", d => new ConditionTask(d, CreateToggleCondition(d)) },
            { "pilot", d => new ConditionTask(d, c => c.IsPiloting && (d.Data.Flight == null || c.IsPilotingFlight == d.Data.Flight)) },
            { "ship_camera", d => new ConditionTask(d, c => c.IsUsingShipCamera) },
            { "enter_gravity", d => new ConditionTask(d, c => c.IsInGravity) },
            { "all_of", d => new CompositeTask(d, CreateChildren(d), true) },
            { "any_of", d => new CompositeTask(d, CreateChildren(d), false) }
        };

    public static readonly string[] ToggleTargets = { "magnet", "flashlight" };

    public static IEnumerable<string> Types => _types.Keys;

    public static bool IsKnown(string type)
    {
        return !string.IsNullOrWhiteSpace(type) && _types.ContainsKey(type);
    }

    public static void Register(string type, Func<TutorialTaskDefinition, ITutorialTask> create)
    {
        if (string.IsNullOrWhiteSpace(type) || create == null) return;
        _types[type] = create;
    }

    public static ITutorialTask Create(TutorialTaskDefinition definition)
    {
        return definition != null && _types.TryGetValue(definition.Type, out var create) ? create(definition) : null;
    }

    private static IReadOnlyList<ITutorialTask> CreateChildren(TutorialTaskDefinition definition)
    {
        var children = new List<ITutorialTask>(definition.Children.Count);
        for (int i = 0; i < definition.Children.Count; i++)
        {
            var child = Create(definition.Children[i]);
            if (child != null) children.Add(child);
        }
        return children;
    }

    private static Func<ITutorialContext, bool> CreateEquipCondition(TutorialTaskDefinition definition)
    {
        if (definition.ItemId != 0) return c => c.IsEquipped(definition.ItemId);
        if (Enum.TryParse(definition.Data.Slot, true, out EquipmentSlotType slot)) return c => c.IsSlotEquipped(slot);
        return c => c.HasAnyEquipment;
    }

    private static Func<ITutorialContext, bool> CreateToggleCondition(TutorialTaskDefinition definition)
    {
        bool state = definition.Data.State;
        return definition.Data.What.ToLowerInvariant() switch
        {
            "magnet" => c => c.IsMagnetEnabled == state,
            "flashlight" => c => c.IsFlashlightOn == state,
            _ => _ => false
        };
    }
}
