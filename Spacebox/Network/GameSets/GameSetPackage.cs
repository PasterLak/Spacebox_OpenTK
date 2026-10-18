using Engine;
using Engine.Networking;
using Spacebox.Game.Resource;

namespace Spacebox.Network;

public static class GameSetPackage
{
    public const string TransferName = "gameset";

    private static string _modPath;
    private static string _hash;
    private static byte[] _archive;

    public static void Register(NetworkManager network)
    {
        network.Transfers.Serve(TransferName, GetArchive);
    }

    public static string FolderName
    {
        get
        {
            string modPath = CurrentModPath;
            return string.IsNullOrEmpty(modPath) ? string.Empty : Path.GetFileName(Path.GetFullPath(modPath).TrimEnd(Path.DirectorySeparatorChar));
        }
    }

    public static string Hash
    {
        get
        {
            Refresh();
            return _hash ?? string.Empty;
        }
    }

    private static string CurrentModPath => GameSetLoader.ModInfo?.ModPath;

    private static byte[] GetArchive()
    {
        Refresh();
        if (_archive != null) return _archive;

        string modPath = CurrentModPath;
        if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath)) throw new InvalidOperationException("No GameSet is loaded");

        _archive = GameSetFolders.Pack(modPath);
        Debug.Log($"[Server] GameSet package ready ({_archive.Length / 1024} KB)");
        return _archive;
    }

    private static void Refresh()
    {
        string modPath = CurrentModPath;
        if (modPath == _modPath && _hash != null) return;

        _modPath = modPath;
        _archive = null;
        _hash = string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath) ? null : GameSetFolders.ComputeHash(modPath);
    }
}
