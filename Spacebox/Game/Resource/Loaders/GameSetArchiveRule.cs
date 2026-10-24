using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource;

public sealed class GameSetArchiveRule : IModArchiveRule
{
    private readonly ModFolders _folders;
    private readonly bool _useModNameAsFolder;

    public GameSetArchiveRule(ModFolders folders, bool useModNameAsFolder)
    {
        _folders = folders;
        _useModNameAsFolder = useModNameAsFolder;
    }

    public string FindContent(string extractedFolder)
    {
        if (File.Exists(Path.Combine(extractedFolder, GameSetFormat.ConfigFile))) return extractedFolder;

        foreach (var subDir in Directory.GetDirectories(extractedFolder))
        {
            if (File.Exists(Path.Combine(subDir, GameSetFormat.ConfigFile))) return subDir;
        }

        return null;
    }

    public ModArchiveTarget ChooseTarget(string contentFolder, string archivePath)
    {
        string configPath = Path.Combine(contentFolder, GameSetFormat.ConfigFile);
        var configNode = File.Exists(configPath) ? ContentJson.ParseFile(configPath, null) : null;
        string modName = _useModNameAsFolder ? ModArchiveConfig.ModName(configNode) : null;
        string folderName = ModInstallTarget.SafeFolderName(modName, ModArchive.NameWithoutExtension(archivePath));
        var config = InstalledGameSets.ReadConfig(contentFolder);
        string root = _folders.FolderFor(GameSetRoles.IsExtension(config?.Extends));
        string installId = ModArchiveConfig.InstallId(configNode, folderName);
        string target = ModInstallTarget.Resolve(root, folderName, installId, folder => InstalledGameSets.ReadConfig(folder)?.ModId, out bool replaces);
        return new ModArchiveTarget(target, replaces);
    }
}
