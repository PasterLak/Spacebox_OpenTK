namespace Spacebox.Game.Tutorial;

public static class TutorialTaskLabels
{
    private static readonly Dictionary<string, string> _withItem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "place_block", "Place {item}" },
        { "destroy_block", "Mine {item}" },
        { "pickup", "Pick up {item}" },
        { "craft", "Craft {item}" },
        { "process", "Get {item} from a machine" },
        { "use_item", "Use {item}" },
        { "use_block", "Open the {item}" },
        { "have_item", "Collect {item}" },
        { "equip", "Equip {item}" },
        { "select_hotbar", "Select {item} in the hotbar" }
    };

    private static readonly Dictionary<string, string> _general = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "press", "Press {key:@action}" },
        { "hold", "Hold {key:@action}" },
        { "wait", "Read the text" },
        { "look", "Look around" },
        { "move", "Travel {target} blocks" },
        { "ship_travel", "Fly {target} blocks with a ship" },
        { "stat", "Progress" },
        { "place_block", "Place blocks" },
        { "destroy_block", "Mine blocks" },
        { "pickup", "Pick up items" },
        { "craft", "Craft items" },
        { "process", "Process items in a machine" },
        { "use_item", "Use an item" },
        { "equip", "Equip something" },
        { "open_ui", "Open the {@window}" },
        { "toggle", "Toggle the {@what}" },
        { "pilot", "Sit at a ship console" },
        { "ship_camera", "Look through a ship camera" },
        { "enter_gravity", "Stand in a gravity field" },
        { "bind_ship_part", "Bind ship parts" },
        { "create_waypoint", "Create a waypoint" },
        { "all_of", "Complete all" },
        { "any_of", "Complete one" }
    };

    public static string Default(TutorialTaskDefinition definition)
    {
        string template;
        if (!string.IsNullOrEmpty(definition.ItemKey) && _withItem.TryGetValue(definition.Type, out var withItem))
            template = withItem.Replace("{item}", "{item:" + definition.ItemKey + "}");
        else if (!_general.TryGetValue(definition.Type, out template))
            template = definition.Type;

        return template
            .Replace("@action", definition.Data.Action ?? "")
            .Replace("{@window}", definition.Data.Window ?? "")
            .Replace("{@what}", definition.Data.What ?? "");
    }
}
