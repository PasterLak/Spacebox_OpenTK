namespace Spacebox.Game.Resource.Content;

public sealed class GameSetStackResult
{
    public GameSetStack Stack { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool Success => Stack != null && Errors.Count == 0;
    public string ErrorText => string.Join("\n", Errors);

    public GameSetStackResult(GameSetStack stack, IReadOnlyList<string> errors)
    {
        Stack = stack;
        Errors = errors;
    }
}

public static class GameSetStackResolver
{
    public static GameSetStackResult Resolve(ModFolders folders, string modId, IReadOnlyList<string> extensionIds, string folderName = null)
    {
        return Resolve(InstalledGameSets.Scan(folders), folders.DefaultGameSet, modId, extensionIds, folderName);
    }

    public static GameSetStackResult Resolve(IReadOnlyList<InstalledGameSet> installed, string defaultFolder, string modId, IReadOnlyList<string> extensionIds, string folderName = null)
    {
        var errors = new List<string>();
        var selected = FindSelected(installed, modId, folderName);
        if (selected == null)
        {
            errors.Add($"GameSet '{(string.IsNullOrWhiteSpace(modId) ? folderName : modId)}' is not installed");
            return new GameSetStackResult(null, errors);
        }

        var requested = new List<string>();
        var baseSet = selected;
        if (selected.IsExtension)
        {
            var targets = ExtensionTargets.Parse(selected.Config.Extends);
            baseSet = ExtensionTargets.IsAny(selected.Config.Extends) || targets.Count == 0 ? null : InstalledGameSets.Find(installed, targets[0]);
            requested.Add(selected.Id);
            if (baseSet == null)
            {
                errors.Add($"Extension '{selected.Id}' extends '{ExtensionTargets.Describe(selected.Config.Extends)}': pick a base GameSet for it");
                return new GameSetStackResult(null, errors);
            }
        }

        if (extensionIds != null)
        {
            foreach (var id in extensionIds)
            {
                if (!requested.Exists(r => string.Equals(r, id, StringComparison.OrdinalIgnoreCase))) requested.Add(id);
            }
        }

        var resolution = ExtensionResolver.Resolve(baseSet, installed, requested);
        errors.AddRange(resolution.Errors);
        var stack = new GameSetStack(baseSet, resolution.Order, GameSetFallbacks.For(baseSet, installed, defaultFolder));
        return new GameSetStackResult(stack, errors);
    }

    private static InstalledGameSet FindSelected(IReadOnlyList<InstalledGameSet> installed, string modId, string folderName)
    {
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            foreach (var gameSet in installed)
            {
                if (string.Equals(gameSet.FolderName, folderName, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(modId) || string.Equals(gameSet.Id, modId, StringComparison.OrdinalIgnoreCase))) return gameSet;
            }
        }

        if (!string.IsNullOrWhiteSpace(modId))
        {
            var byId = InstalledGameSets.Find(installed, modId);
            if (byId != null) return byId;
        }

        return string.IsNullOrWhiteSpace(folderName) ? null : InstalledGameSets.Find(installed, folderName);
    }
}
