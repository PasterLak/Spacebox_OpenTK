namespace Spacebox.Game.Resource.TexturePacks;

public static class TexturePackOrder
{
    public static bool IsEnabled(IReadOnlyList<string> enabled, string id)
    {
        return IndexOf(enabled, id) >= 0;
    }

    public static void Toggle(List<string> enabled, string id)
    {
        int index = IndexOf(enabled, id);
        if (index >= 0) enabled.RemoveAt(index);
        else enabled.Insert(0, id);
    }

    public static bool MoveUp(List<string> enabled, string id)
    {
        int index = IndexOf(enabled, id);
        if (index <= 0) return false;
        (enabled[index - 1], enabled[index]) = (enabled[index], enabled[index - 1]);
        return true;
    }

    public static bool MoveDown(List<string> enabled, string id)
    {
        int index = IndexOf(enabled, id);
        if (index < 0 || index >= enabled.Count - 1) return false;
        (enabled[index + 1], enabled[index]) = (enabled[index], enabled[index + 1]);
        return true;
    }

    public static int IndexOf(IReadOnlyList<string> enabled, string id)
    {
        for (int i = 0; i < enabled.Count; i++)
        {
            if (string.Equals(enabled[i], id, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }
}
