using System.Text;
using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content;

public enum NewGameSetKind
{
    GameSet,
    Extension
}

public sealed class NewGameSetRequest
{
    public NewGameSetKind Kind { get; init; }
    public string Name { get; init; } = "";
    public string Author { get; init; } = "";
    public string BaseId { get; init; } = "";
}

public sealed class NewGameSetResult
{
    private NewGameSetResult(string folder, string modId, string error)
    {
        Folder = folder;
        ModId = modId;
        Error = error;
    }

    public string Folder { get; }
    public string ModId { get; }
    public string Error { get; }
    public bool Success => Error == null;

    public static NewGameSetResult Created(string folder, string modId) => new NewGameSetResult(folder, modId, null);

    public static NewGameSetResult Failed(string error) => new NewGameSetResult(null, null, error);
}

public static class GameSetCreator
{
    public const int MaxNameLength = 40;
    private const string IconFile = "icon.png";
    private const string FirstVersion = "1.0";

    public static string Validate(NewGameSetRequest request, IReadOnlyList<InstalledGameSet> installed)
    {
        string name = request.Name?.Trim() ?? "";
        if (name.Length == 0) return "Enter a name";
        if (name.Length > MaxNameLength) return $"The name can have up to {MaxNameLength} characters";
        if (MakeId(name).Length == 0) return "The name needs at least one letter or digit";
        if (request.Kind == NewGameSetKind.Extension && FindBase(request.BaseId, installed) == null) return "Pick the GameSet to extend";
        return null;
    }

    public static IReadOnlyList<InstalledGameSet> ExtendableBases(IReadOnlyList<InstalledGameSet> installed)
    {
        var result = new List<InstalledGameSet>();
        foreach (var gameSet in installed)
            if (!gameSet.IsExtension && !gameSet.IsEmpty) result.Add(gameSet);
        return result;
    }

    public static NewGameSetResult Create(ModFolders folders, NewGameSetRequest request, IReadOnlyList<InstalledGameSet> installed)
    {
        string error = Validate(request, installed);
        if (error != null) return NewGameSetResult.Failed(error);

        string name = request.Name.Trim();
        var baseSet = request.Kind == NewGameSetKind.Extension ? FindBase(request.BaseId, installed) : null;
        string modId = UniqueId(MakeId(name), installed);
        string root = folders.FolderFor(request.Kind == NewGameSetKind.Extension);
        string folder = ModInstallTarget.Resolve(root, ModInstallTarget.SafeFolderName(name, modId), null, _ => null, out _);

        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, GameSetFormat.ConfigFile), Config(modId, name, request.Author, baseSet).ToJsonString(ContentJson.Indented));
            CopyIcon(baseSet ?? FindEmpty(installed), folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return NewGameSetResult.Failed("Could not create the folder: " + ex.Message);
        }

        return NewGameSetResult.Created(folder, modId);
    }

    public static string MakeId(string name)
    {
        var builder = new StringBuilder();
        bool underscore = false;
        foreach (char c in (name ?? "").Trim().ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(c);
                underscore = false;
            }
            else if (builder.Length > 0 && !underscore)
            {
                builder.Append('_');
                underscore = true;
            }
        }
        return builder.ToString().TrimEnd('_');
    }

    public static string UniqueId(string id, IReadOnlyList<InstalledGameSet> installed)
    {
        if (!Taken(id, installed)) return id;
        for (int i = 2; ; i++)
        {
            string candidate = $"{id}_{i}";
            if (!Taken(candidate, installed)) return candidate;
        }
    }

    private static bool Taken(string id, IReadOnlyList<InstalledGameSet> installed)
    {
        if (GameSetRoles.IsEmpty(id) || string.Equals(id, Globals.GameSet.Default, StringComparison.OrdinalIgnoreCase)) return true;
        return InstalledGameSets.Find(installed, id) != null;
    }

    private static JsonObject Config(string modId, string name, string author, InstalledGameSet baseSet)
    {
        var config = new JsonObject
        {
            ["ModId"] = modId,
            ["ModName"] = name,
            ["Description"] = "",
            ["Author"] = author?.Trim() ?? "",
            ["Version"] = FirstVersion,
            ["Extends"] = baseSet?.Id ?? Globals.GameSet.EmptyId
        };
        if (baseSet != null && !string.IsNullOrWhiteSpace(baseSet.Config.Version)) config["MinBaseVersion"] = baseSet.Config.Version;
        return config;
    }

    private static InstalledGameSet FindBase(string id, IReadOnlyList<InstalledGameSet> installed)
    {
        var baseSet = string.IsNullOrWhiteSpace(id) ? null : InstalledGameSets.Find(installed, id);
        return baseSet == null || baseSet.IsExtension || baseSet.IsEmpty ? null : baseSet;
    }

    private static InstalledGameSet FindEmpty(IReadOnlyList<InstalledGameSet> installed)
    {
        foreach (var gameSet in installed)
            if (gameSet.IsEmpty) return gameSet;
        return null;
    }

    private static void CopyIcon(InstalledGameSet source, string folder)
    {
        string icon = source == null ? null : Path.Combine(source.Folder, IconFile);
        if (icon != null && File.Exists(icon)) File.Copy(icon, Path.Combine(folder, IconFile), true);
    }
}
