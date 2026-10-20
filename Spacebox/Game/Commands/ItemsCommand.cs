using Engine;
using Engine.Commands;

namespace Spacebox.Game.Commands;

public sealed class ItemsCommand : CommandBase
{
    private const int MaxLines = 30;

    private readonly List<string> _ids = new List<string>();

    public override string Name => "items";

    public override string Description => "List item ids for give. Usage: items [filter]";

    public override void Execute(string[] args)
    {
        string filter = args.Length > 0 ? args[0] : "";
        _ids.Clear();
        CommandSuggestionSources.AddItemIds(_ids);
        _ids.Sort(StringComparer.OrdinalIgnoreCase);

        int shown = 0;
        int matches = 0;
        for (int i = 0; i < _ids.Count; i++)
        {
            if (filter.Length > 0 && _ids[i].IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
            matches++;
            if (shown >= MaxLines) continue;

            string name = ItemLookup.TryFind(_ids[i], out var item) ? item.Name : "";
            Debug.Log($"{_ids[i]} - {name}");
            shown++;
        }

        if (matches == 0) Debug.Warning($"No items match '{filter}'");
        else if (matches > shown) Debug.Log($"... {matches - shown} more, add a filter: items <text>");
    }
}
