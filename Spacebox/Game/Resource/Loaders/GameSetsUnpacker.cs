using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource;

internal static class GameSetsUnpacker
{
    public static void UnpackMods(bool useModNameAsFolder)
    {
        var folders = ModFolders.Local;
        var gameSets = new GameSetArchiveRule(folders, useModNameAsFolder);
        foreach (var folder in folders.ContentFolders)
        {
            ModArchiveUnpacker.UnpackFolder(folder, gameSets);
        }

        ModArchiveUnpacker.UnpackFolder(folders.TexturePacks, new TexturePackArchiveRule(folders.TexturePacks));
    }
}
