using Engine;
using Spacebox.Game.Resource.Content;
using Spacebox.Network;

namespace Spacebox.Game.Resource;

public static class GameFolderMigration
{
    private const string OldGameSetsFolder = "GameSets";
    private const string OldTexturePacksFolder = "TexturePacks";
    private const string OldServerCacheFolder = "Server";
    private const string OldServerCacheGameSet = "GameSet";
    private const string OldDashboardFolder = "dashboard";
    private const string ClientConfigFile = "client.json";
    private const string CommandHistoryFile = "command_history.txt";
    private const string DebugFolder = "Debug";
    private const string LeftoverSuffix = ".old";

    private static readonly string[] ServerIconFiles = { "server-icon.png", "server-icon.jpg", "server-icon.jpeg" };
    private static readonly string[] MultiplayerFolderNames =
    {
        MultiplayerFolders.WorldsName, MultiplayerFolders.ModsName, MultiplayerFolders.DashboardName, MultiplayerFolders.ServerIconsName
    };

    public static IReadOnlyList<string> Run(string baseDirectory)
    {
        var log = new List<string>();
        var mods = new ModFolders(Path.Combine(baseDirectory, ModFolders.RootName));
        string multiplayer = Path.Combine(baseDirectory, MultiplayerFolders.RootName);

        MoveGameSets(Path.Combine(baseDirectory, OldGameSetsFolder), mods, log);
        MoveChildren(Path.Combine(baseDirectory, OldTexturePacksFolder), mods.TexturePacks, log);
        DeleteServerCaches(Path.Combine(baseDirectory, OldServerCacheFolder), log);
        MoveWorldCaches(multiplayer, log);
        MoveFile(Path.Combine(baseDirectory, ClientConfigFile), Path.Combine(multiplayer, ClientConfigFile), false, log);
        MoveFile(Path.Combine(baseDirectory, Network.Dedicated.ServerConfig.FileName), Path.Combine(multiplayer, Network.Dedicated.ServerConfig.FileName), true, log);
        foreach (var icon in ServerIconFiles) MoveFile(Path.Combine(baseDirectory, icon), Path.Combine(multiplayer, icon), false, log);
        MoveDashboard(Path.Combine(baseDirectory, OldDashboardFolder), Path.Combine(multiplayer, MultiplayerFolders.DashboardName), log);
        MoveFile(Path.Combine(baseDirectory, CommandHistoryFile), Path.Combine(baseDirectory, DebugFolder, CommandHistoryFile), false, log);

        foreach (var line in log) Debug.Log("[Folders] " + line);
        return log;
    }

    private static void MoveGameSets(string source, ModFolders mods, List<string> log)
    {
        if (!Directory.Exists(source)) return;

        foreach (var folder in Directory.GetDirectories(source))
        {
            bool isExtension = !string.IsNullOrWhiteSpace(InstalledGameSets.ReadConfig(folder)?.Extends);
            MoveDirectory(folder, Path.Combine(mods.FolderFor(isExtension), Path.GetFileName(folder)), log);
        }
        foreach (var file in Directory.GetFiles(source)) MoveFile(file, Path.Combine(mods.GameSets, Path.GetFileName(file)), false, log);
        SetAsideLeftovers(source, log);
    }

    private static void MoveChildren(string source, string target, List<string> log)
    {
        if (!Directory.Exists(source)) return;

        foreach (var folder in Directory.GetDirectories(source)) MoveDirectory(folder, Path.Combine(target, Path.GetFileName(folder)), log);
        foreach (var file in Directory.GetFiles(source)) MoveFile(file, Path.Combine(target, Path.GetFileName(file)), false, log);
        SetAsideLeftovers(source, log);
    }

    private static void SetAsideLeftovers(string source, List<string> log)
    {
        DeleteIfEmpty(source);
        if (!Directory.Exists(source)) return;

        string target = source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + LeftoverSuffix;
        if (Directory.Exists(target)) return;
        Try(() => Directory.Move(source, target), $"Renamed {source} to {target}, delete it when it is not needed", log);
    }

    private static void DeleteServerCaches(string source, List<string> log)
    {
        if (!Directory.Exists(source)) return;

        foreach (var server in Directory.GetDirectories(source))
        {
            string cache = Path.Combine(server, OldServerCacheGameSet);
            if (!Directory.Exists(cache)) continue;

            Try(() => Directory.Delete(cache, true), $"Removed the old server GameSet cache {cache}", log);
            DeleteIfEmpty(server);
        }
        DeleteIfEmpty(source);
    }

    private static void MoveWorldCaches(string multiplayer, List<string> log)
    {
        if (!Directory.Exists(multiplayer)) return;

        string worlds = Path.Combine(multiplayer, MultiplayerFolders.WorldsName);
        foreach (var folder in Directory.GetDirectories(multiplayer))
        {
            string name = Path.GetFileName(folder);
            if (Array.Exists(MultiplayerFolderNames, known => string.Equals(known, name, StringComparison.OrdinalIgnoreCase))) continue;
            MoveDirectory(folder, Path.Combine(worlds, name), log);
        }
    }

    private static void MoveDashboard(string source, string target, List<string> log)
    {
        if (!Directory.Exists(source) || SamePath(source, target)) return;
        MoveChildren(source, target, log);
    }

    private static void MoveDirectory(string source, string target, List<string> log)
    {
        if (!HasFiles(source))
        {
            Try(() => Directory.Delete(source, true), $"Removed the empty folder {source}", log);
            return;
        }

        if (Directory.Exists(target))
        {
            log.Add($"Kept {source}: {target} already exists");
            return;
        }

        Try(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            Directory.Move(source, target);
        }, $"Moved {source} to {target}", log);
    }

    private static void MoveFile(string source, string target, bool replace, List<string> log)
    {
        if (!File.Exists(source)) return;
        if (File.Exists(target) && !replace)
        {
            log.Add($"Kept {source}: {target} already exists");
            return;
        }

        Try(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Move(source, target, replace);
        }, $"Moved {source} to {target}", log);
    }

    private static bool HasFiles(string folder)
    {
        return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Any();
    }

    private static void DeleteIfEmpty(string folder)
    {
        try
        {
            if (Directory.Exists(folder) && Directory.GetFileSystemEntries(folder).Length == 0) Directory.Delete(folder);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void Try(Action action, string success, List<string> log)
    {
        try
        {
            action();
            log.Add(success);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            log.Add($"Not done ({ex.Message}): {success}");
        }
    }

    private static bool SamePath(string a, string b)
    {
        return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }
}
