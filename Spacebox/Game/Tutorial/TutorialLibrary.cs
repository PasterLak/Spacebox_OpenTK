using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Tutorial;

public static class TutorialLibrary
{
    private static readonly List<TutorialDefinition> _tutorials = new List<TutorialDefinition>();

    public static IReadOnlyList<TutorialDefinition> All => _tutorials;

    public static void Set(IEnumerable<TutorialDefinition> tutorials)
    {
        _tutorials.Clear();
        if (tutorials != null) _tutorials.AddRange(tutorials);
        _tutorials.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Id, b.Id));
    }

    public static void Clear()
    {
        _tutorials.Clear();
    }

    public static TutorialDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        for (int i = 0; i < _tutorials.Count; i++)
        {
            if (string.Equals(_tutorials[i].Id, id, StringComparison.OrdinalIgnoreCase)) return _tutorials[i];
        }
        return null;
    }

    public static TutorialDefinition FindFor(GameMode mode)
    {
        for (int i = 0; i < _tutorials.Count; i++)
        {
            if (_tutorials[i].SupportsMode(mode)) return _tutorials[i];
        }
        return null;
    }
}
