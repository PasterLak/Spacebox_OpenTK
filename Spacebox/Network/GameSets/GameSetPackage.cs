using Engine;
using Engine.Networking;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Network;

public static class GameSetPackage
{
    public const string TransferName = "gameset";

    private static string _stackKey;
    private static string _hash;
    private static byte[] _archive;
    private static IReadOnlyList<TexturePack> _texturePacks = Array.Empty<TexturePack>();
    private static readonly object Sync = new object();

    public static void Register(NetworkManager network)
    {
        network.Transfers.Serve(TransferName, GetArchive);
    }

    public static void PrepareInBackground()
    {
        Engine.Multithreading.WorkerPoolManager.Enqueue(_ =>
        {
            try
            {
                GetArchive();
            }
            catch (Exception ex)
            {
                Debug.Warning($"[Server] GameSet package could not be prepared: {ex.Message}");
            }
        }, Engine.Multithreading.WorkerPoolManager.Priority.Low);
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

    public static string[] TexturePackIds
    {
        get
        {
            Refresh();
            return ServerTexturePacks.IdsOf(_texturePacks);
        }
    }

    private static string CurrentModPath => GameSetLoader.ModInfo?.ModPath;
    private static IReadOnlyList<InstalledGameSet> Extensions => GameSetLoader.Stack?.Extensions ?? Array.Empty<InstalledGameSet>();
    private static string StackKey => CurrentModPath + "|" + WorldExtensionList.Format(GameSetLoader.Stack?.ExtensionIds) + "|" + WorldExtensionList.Format(ServerTexturePacks.Ids);

    private static byte[] GetArchive()
    {
        lock (Sync)
        {
            return BuildArchive();
        }
    }

    private static byte[] BuildArchive()
    {
        Refresh();
        if (_archive != null) return _archive;

        string modPath = CurrentModPath;
        if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath)) throw new InvalidOperationException("No GameSet is loaded");

        _archive = GameSetFolders.Pack(modPath, Extensions, _texturePacks);
        Debug.Log($"[Server] GameSet package ready ({_archive.Length / 1024} KB)");
        return _archive;
    }

    private static void Refresh()
    {
        lock (Sync)
        {
            RefreshLocked();
        }
    }

    private static void RefreshLocked()
    {
        string modPath = CurrentModPath;
        string key = StackKey;
        if (key == _stackKey && _hash != null) return;

        _stackKey = key;
        _archive = null;
        _texturePacks = ServerTexturePacks.Resolve();
        _hash = string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath) ? null : GameSetFolders.ComputeHash(modPath, Extensions, _texturePacks);
    }
}
