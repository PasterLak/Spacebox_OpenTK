using Engine;
using Engine.Networking;
using Engine.Utils;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Network;

public static class GameSetDownloader
{
    public static void Prepare(RemoteWorldInfo world, Action<float> progress, Action completed, Action<string> failed)
    {
        world.GameSetCacheName = ServerFolderName.Current;

        if (TryUse(world, GameSetLocator.Local, true, true) || TryUse(world, CacheFolders(world), false, true))
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

    private static bool TryUse(RemoteWorldInfo world, ModFolders folders, bool local, bool verify)
    {
        if (!TryResolve(folders, world, out var stack, out var packs)) return false;
        if (verify && !Matches(stack, packs, world.GameSetHash)) return false;

        world.UseLocalGameSet = local;
        world.GameSetFolder = stack.Base.FolderName;
        world.TexturePackFolders = FoldersOf(packs);
        Debug.Log($"[Network] Using {(local ? "local" : "server")} GameSet '{world.GameSetFolder}'{Describe(world)}");
        return true;
    }

    private static void Install(RemoteWorldInfo world, byte[] data)
    {
        var folders = CacheFolders(world);
        string root = Path.GetDirectoryName(folders.GameSets);
        if (Directory.Exists(root)) Directory.Delete(root, true);

        DirectoryArchive.Extract(data, root);
        Debug.Success($"[Network] GameSet downloaded ({data.Length / 1024} KB)");

        if (TryUse(world, folders, false, true)) return;

        var result = GameSetStackResolver.Resolve(folders, world.ModId, world.Extensions);
        if (!result.Success)
            throw new InvalidDataException($"The server GameSet '{world.ModId}'{Describe(world)} is incomplete: {string.Join("; ", result.Errors)}");
        if (!TryUse(world, folders, false, false))
            throw new InvalidDataException($"Texture packs {string.Join(", ", world.TexturePacks)} are missing in the download");

        Debug.Warning("[Network] Downloaded GameSet hash differs from the server hash");
    }

    public static bool TryResolve(ModFolders folders, RemoteWorldInfo world, out GameSetStack stack, out IReadOnlyList<TexturePack> packs)
    {
        stack = null;
        packs = Array.Empty<TexturePack>();
        if (!Directory.Exists(folders.GameSets)) return false;

        var result = GameSetStackResolver.Resolve(folders, world.ModId, world.Extensions);
        if (!result.Success) return false;

        var ids = world.TexturePacks ?? Array.Empty<string>();
        var found = TexturePackCatalog.Active(TexturePackCatalog.Scan(folders.TexturePacks), ids);
        if (found.Count != ids.Length) return false;

        stack = result.Stack;
        packs = found;
        return true;
    }

    private static string Describe(RemoteWorldInfo world)
    {
        string text = world.Extensions == null || world.Extensions.Length == 0 ? "" : $" with extensions {string.Join(", ", world.Extensions)}";
        if (world.TexturePacks != null && world.TexturePacks.Length > 0) text += $" and texture packs {string.Join(", ", world.TexturePacks)}";
        return text;
    }

    private static string[] FoldersOf(IReadOnlyList<TexturePack> packs)
    {
        var folders = new string[packs.Count];
        for (int i = 0; i < packs.Count; i++) folders[i] = packs[i].Folder;
        return folders;
    }

    private static bool Matches(GameSetStack stack, IReadOnlyList<TexturePack> packs, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash)) return true;

        try
        {
            return string.Equals(GameSetFolders.ComputeHash(stack, packs), expectedHash, StringComparison.OrdinalIgnoreCase);
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

    private static ModFolders CacheFolders(RemoteWorldInfo world)
    {
        return MultiplayerFolders.ModCache(world.GameSetCacheName);
    }
}
