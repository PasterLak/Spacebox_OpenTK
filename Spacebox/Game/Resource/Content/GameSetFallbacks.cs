namespace Spacebox.Game.Resource.Content;

public static class GameSetFallbacks
{
    public static IReadOnlyList<InstalledGameSet> For(InstalledGameSet baseSet, IReadOnlyList<InstalledGameSet> installed, string defaultFolder)
    {
        var result = new List<InstalledGameSet>();
        if (baseSet == null || baseSet.IsEmpty) return result;

        if (!baseSet.IsBasedOnEmpty)
        {
            var defaultSet = FindByFolder(installed, defaultFolder);
            if (defaultSet != null && !SameFolder(defaultSet, baseSet) && !defaultSet.IsExtension) result.Add(defaultSet);
        }

        var empty = FindEmpty(installed);
        if (empty != null && !SameFolder(empty, baseSet)) result.Add(empty);
        return result;
    }

    private static InstalledGameSet FindEmpty(IReadOnlyList<InstalledGameSet> installed)
    {
        foreach (var gameSet in installed)
            if (gameSet.IsEmpty) return gameSet;
        return null;
    }

    private static InstalledGameSet FindByFolder(IReadOnlyList<InstalledGameSet> installed, string folder)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return null;
        foreach (var gameSet in installed)
            if (GameSetStack.SameFolder(gameSet.Folder, folder)) return gameSet;
        return null;
    }

    private static bool SameFolder(InstalledGameSet a, InstalledGameSet b) => GameSetStack.SameFolder(a.Folder, b.Folder);
}
