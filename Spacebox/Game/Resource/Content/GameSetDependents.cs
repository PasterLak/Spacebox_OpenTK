namespace Spacebox.Game.Resource.Content;

public sealed record WorldGameSetUse(string Name, string ModId, IReadOnlyList<string> Extensions);

public sealed class GameSetDependents
{
    private GameSetDependents(IReadOnlyList<InstalledGameSet> mods, IReadOnlyList<string> worlds)
    {
        Mods = mods;
        Worlds = worlds;
    }

    public IReadOnlyList<InstalledGameSet> Mods { get; }
    public IReadOnlyList<string> Worlds { get; }
    public bool Any => Mods.Count > 0 || Worlds.Count > 0;

    public static bool CanDelete(InstalledGameSet gameSet)
    {
        return gameSet != null && !gameSet.IsEmpty && !string.Equals(gameSet.FolderName, Globals.GameSet.Default, StringComparison.OrdinalIgnoreCase);
    }

    public static GameSetDependents Find(InstalledGameSet target, IReadOnlyList<InstalledGameSet> installed, IEnumerable<WorldGameSetUse> worlds)
    {
        var broken = new HashSet<InstalledGameSet> { target };
        bool added = true;
        while (added)
        {
            added = false;
            foreach (var gameSet in installed)
            {
                if (broken.Contains(gameSet) || !DependsOnAny(gameSet, broken)) continue;
                broken.Add(gameSet);
                added = true;
            }
        }

        var mods = new List<InstalledGameSet>();
        foreach (var gameSet in installed)
            if (!ReferenceEquals(gameSet, target) && broken.Contains(gameSet)) mods.Add(gameSet);

        var worldNames = new List<string>();
        foreach (var world in worlds ?? Array.Empty<WorldGameSetUse>())
        {
            if (UsesAny(world, broken)) worldNames.Add(world.Name);
        }
        return new GameSetDependents(mods, worldNames);
    }

    private static bool DependsOnAny(InstalledGameSet gameSet, HashSet<InstalledGameSet> broken)
    {
        foreach (var dependency in broken)
        {
            if (gameSet.IsBasedOnEmpty && dependency.IsEmpty) return true;
            if (!gameSet.IsExtension) continue;
            foreach (var target in ExtensionTargets.Parse(gameSet.Config.Extends))
                if (dependency.Matches(target)) return true;
            foreach (var include in gameSet.Includes)
                if (dependency.Matches(include)) return true;
        }
        return false;
    }

    private static bool UsesAny(WorldGameSetUse world, HashSet<InstalledGameSet> broken)
    {
        foreach (var gameSet in broken)
        {
            if (gameSet.Matches(world.ModId)) return true;
            foreach (var extension in world.Extensions ?? Array.Empty<string>())
                if (gameSet.Matches(extension)) return true;
        }
        return false;
    }
}
