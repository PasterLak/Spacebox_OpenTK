using Engine;
using Engine.Networking;
using Engine.Utils;
using Spacebox.Game.Generation;
using Spacebox.Scenes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Network;

public static class GameSetDownloader
{
    public static void Prepare(RemoteWorldInfo world, Action<float> progress, Action completed, Action<string> failed)
    {
        world.GameSetCacheName = ServerFolderName.Current;

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
        var stack = Resolve(GameSetLocator.LocalDirectory, world);
        if (stack == null || !Matches(stack, world.GameSetHash)) return false;

        world.UseLocalGameSet = true;
        world.GameSetFolder = stack.Base.FolderName;
        Debug.Log($"[Network] Using local GameSet '{world.GameSetFolder}'{ExtensionText(world)}");
        return true;
    }

    private static bool TryUseCache(RemoteWorldInfo world, bool verify)
    {
        var stack = Resolve(CacheDirectory(world), world);
        if (stack == null) return false;
        if (verify && !Matches(stack, world.GameSetHash)) return false;

        world.UseLocalGameSet = false;
        world.GameSetFolder = stack.Base.FolderName;
        Debug.Log($"[Network] Using server GameSet '{world.GameSetFolder}'{ExtensionText(world)} from {CacheDirectory(world)}");
        return true;
    }

    private static void Install(RemoteWorldInfo world, byte[] data)
    {
        string root = CacheDirectory(world);
        if (Directory.Exists(root)) Directory.Delete(root, true);

        DirectoryArchive.Extract(data, root);
        Debug.Success($"[Network] GameSet downloaded ({data.Length / 1024} KB)");

        if (TryUseCache(world, true)) return;

        var result = GameSetStackResolver.Resolve(root, world.ModId, world.Extensions);
        if (!result.Success)
            throw new InvalidDataException($"The server GameSet '{world.ModId}'{ExtensionText(world)} is incomplete: {string.Join("; ", result.Errors)}");
        if (!TryUseCache(world, false)) throw new InvalidDataException($"GameSet '{world.ModId}' is missing in the download");

        Debug.Warning("[Network] Downloaded GameSet hash differs from the server hash");
    }

    private static GameSetStack Resolve(string directory, RemoteWorldInfo world)
    {
        if (!Directory.Exists(directory)) return null;
        var result = GameSetStackResolver.Resolve(directory, world.ModId, world.Extensions);
        return result.Success ? result.Stack : null;
    }

    private static string ExtensionText(RemoteWorldInfo world)
    {
        return world.Extensions == null || world.Extensions.Length == 0 ? "" : $" with extensions {string.Join(", ", world.Extensions)}";
    }

    private static bool Matches(GameSetStack stack, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash)) return true;

        try
        {
            return string.Equals(GameSetFolders.ComputeHash(stack), expectedHash, StringComparison.OrdinalIgnoreCase);
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
