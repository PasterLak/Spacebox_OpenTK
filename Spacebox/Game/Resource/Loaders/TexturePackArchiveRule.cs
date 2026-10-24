using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Game.Resource;

public sealed class TexturePackArchiveRule : IModArchiveRule
{
    private static readonly string[] ContentFolders = { GameSetFormat.BlocksFolder, GameSetFormat.ItemsFolder, GameSetFormat.EmissionsFolder, TexturePack.UIFolder };

    private readonly string _root;

    public TexturePackArchiveRule(string root)
    {
        _root = root;
    }

    public string FindContent(string extractedFolder)
    {
        if (IsPack(extractedFolder)) return extractedFolder;

        foreach (var subDir in Directory.GetDirectories(extractedFolder))
        {
            if (IsPack(subDir)) return subDir;
        }

        return null;
    }

    public ModArchiveTarget ChooseTarget(string contentFolder, string archivePath)
    {
        var info = TexturePack.Load(contentFolder)?.Info;
        string folderName = ModInstallTarget.SafeFolderName(info?.Name, ModArchive.NameWithoutExtension(archivePath));
        string id = string.IsNullOrWhiteSpace(info?.Id) ? folderName : info.Id;
        string target = ModInstallTarget.Resolve(_root, folderName, id, folder => TexturePack.Load(folder)?.Id, out bool replaces);
        return new ModArchiveTarget(target, replaces);
    }

    private static bool IsPack(string folder)
    {
        if (File.Exists(Path.Combine(folder, TexturePack.InfoFile))) return true;
        for (int i = 0; i < ContentFolders.Length; i++)
        {
            if (Directory.Exists(Path.Combine(folder, ContentFolders[i]))) return true;
        }
        return false;
    }
}
