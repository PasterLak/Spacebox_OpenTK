namespace Spacebox.Game.Resource.Content;

public static class WorldExtensionList
{
    private const char Separator = ',';

    public static string[] Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return Normalize(text.Split(Separator));
    }

    public static string Format(IEnumerable<string> ids)
    {
        return ids == null ? string.Empty : string.Join(Separator, Normalize(ids));
    }

    public static string[] Normalize(IEnumerable<string> ids)
    {
        var result = new List<string>();
        if (ids == null) return result.ToArray();

        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            string trimmed = id.Trim();
            if (!result.Exists(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase))) result.Add(trimmed);
        }
        return result.ToArray();
    }
}
