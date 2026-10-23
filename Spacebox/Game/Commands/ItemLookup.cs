using Spacebox.Game.Resource;

namespace Spacebox.Game.Commands;

public static class ItemLookup
{
    public static string Resolve(IEnumerable<string> fullIds, string query, string modId)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        string trimmed = query.Trim();
        string withNamespace = trimmed.Contains(':') ? trimmed : $"{modId}:{trimmed}";
        string shortMatch = null;
        int shortMatches = 0;

        foreach (var fullId in fullIds)
        {
            if (string.Equals(fullId, trimmed, StringComparison.OrdinalIgnoreCase) || string.Equals(fullId, withNamespace, StringComparison.OrdinalIgnoreCase)) return fullId;

            if (string.Equals(ShortId(fullId), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                shortMatch = fullId;
                shortMatches++;
            }
        }

        return shortMatches == 1 ? shortMatch : null;
    }

    public static string ShortId(string fullId)
    {
        int colon = fullId.IndexOf(':');
        return colon < 0 ? fullId : fullId.Substring(colon + 1);
    }

    public static string DisplayId(string fullId, string modId)
    {
        return fullId.StartsWith(modId + ":", StringComparison.OrdinalIgnoreCase) ? ShortId(fullId) : fullId;
    }

    public static bool TryFind(string query, out Item item)
    {
        item = null;
        string fullId = Resolve(GameAssets.ItemsStr.Keys, query, GameSetLoader.ModInfo?.ModId ?? "");
        if (fullId != null) return GameAssets.ItemsStr.TryGetValue(fullId, out item);
        return !string.IsNullOrWhiteSpace(query) && GameAssets.TryGetItemByFullID(query.Trim(), out item);
    }
}
