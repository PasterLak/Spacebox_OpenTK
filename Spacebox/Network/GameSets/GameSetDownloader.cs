using Engine;
using Engine.Networking;
using Engine.Utils;
using Spacebox.Game.Generation;
using Spacebox.Scenes;

namespace Spacebox.Network;

public static class GameSetDownloader
{
    public static void Prepare(RemoteWorldInfo world, Action<float> progress, Action completed, Action<string> failed)
    {
        world.GameSetCacheName = World.MakeSafeFolderName($"{GameSession.HostAddress}_{GameSession.Port}");

        if (TryUseLocal(world) || TryUseCache(world, true))
        {
            completed();
            return;
        }

        Debug.Log("[Network] Downloading the server GameSet");
        NetworkManager.Instance.Transfers.Request(GameSetPackage.TransferName, data =>
        {
            try
            {
                Install(world, data);
            }
            catch (Exception ex)
            {
                failed($"GameSet install failed: {ex.Message}");
                return;
            }

            completed();
        }, failed, progress);
    }

    private static bool TryUseLocal(RemoteWorldInfo world)
    {
        if (!GameSetLocator.TryFindFolderPath(GameSetLocator.LocalDirectory, world.ModId, out var path) || !Matches(path, world.GameSetHash)) return false;

        world.UseLocalGameSet = true;
        world.GameSetFolder = Path.GetFileName(path);
        Debug.Log($"[Network] Using local GameSet '{world.GameSetFolder}'");
        return true;
    }

    private static bool TryUseCache(RemoteWorldInfo world, bool verify)
    {
        if (!GameSetLocator.TryFindFolderPath(CacheDirectory(world), world.ModId, out var path)) return false;
        if (verify && !Matches(path, world.GameSetHash)) return false;

        world.UseLocalGameSet = false;
        world.GameSetFolder = Path.GetFileName(path);
        Debug.Log($"[Network] Using server GameSet '{world.GameSetFolder}' from {CacheDirectory(world)}");
        return true;
    }

    private static void Install(RemoteWorldInfo world, byte[] data)
    {
        string root = CacheDirectory(world);
        if (Directory.Exists(root)) Directory.Delete(root, true);

        DirectoryArchive.Extract(data, root);
        Debug.Success($"[Network] GameSet downloaded ({data.Length / 1024} KB)");

        if (TryUseCache(world, true)) return;
        if (!TryUseCache(world, false)) throw new InvalidDataException($"GameSet '{world.ModId}' is missing in the download");

        Debug.Warning("[Network] Downloaded GameSet hash differs from the server hash");
    }

    private static bool Matches(string modPath, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash)) return true;

        try
        {
            return string.Equals(GameSetFolders.ComputeHash(modPath), expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string CacheDirectory(RemoteWorldInfo world)
    {
        return ModPath.GetModsPath(false, world.GameSetCacheName);
    }
}
