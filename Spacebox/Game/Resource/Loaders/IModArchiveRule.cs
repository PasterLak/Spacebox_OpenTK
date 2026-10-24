namespace Spacebox.Game.Resource;

public interface IModArchiveRule
{
    string FindContent(string extractedFolder);
    ModArchiveTarget ChooseTarget(string contentFolder, string archivePath);
}

public readonly record struct ModArchiveTarget(string Folder, bool Replaces);
