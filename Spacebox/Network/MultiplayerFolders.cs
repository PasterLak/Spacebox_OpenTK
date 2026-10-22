using Spacebox.Game.Generation;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Network;

public static class MultiplayerFolders
{
    public const string RootName = "Multiplayer";
    public const string WorldsName = "Worlds";
    public const string ModsName = "Mods";
    public const string DashboardName = "Dashboard";
    public const string ServerIconsName = "ServerIcons";

    public static string Root => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RootName);
    public static string Worlds => Path.Combine(Root, WorldsName);
    public static string Mods => Path.Combine(Root, ModsName);
    public static string Dashboard => Path.Combine(Root, DashboardName);
    public static string ServerIcons => Path.Combine(Root, ServerIconsName);

    public static string File(string name) => Path.Combine(Root, name);

    public static string WorldCache(string serverFolder, string worldName) => Path.Combine(Worlds, serverFolder, World.MakeSafeFolderName(worldName));

    public static ModFolders ModCache(string serverFolder) => new ModFolders(Path.Combine(Mods, serverFolder));

    public static string ResolveConfigPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        if (Path.IsPathRooted(path)) return path;

        string inFolder = File(path);
        string nextToGame = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        return !System.IO.File.Exists(inFolder) && System.IO.File.Exists(nextToGame) ? nextToGame : inFolder;
    }
}
