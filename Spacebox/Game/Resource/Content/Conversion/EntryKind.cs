namespace Spacebox.Game.Resource.Content.Conversion;

public sealed class EntryKind
{
    public static readonly EntryKind Blocks = new EntryKind("Block", GameSetFormat.BlocksFile, GameSetFormat.BlocksFolder, GameSetFormat.BlockEntryFile);
    public static readonly EntryKind Items = new EntryKind("Item", GameSetFormat.ItemsFile, GameSetFormat.ItemsFolder, GameSetFormat.ItemEntryFile);

    public string Name { get; }
    public string FlatFile { get; }
    public string Folder { get; }
    public string EntryFile { get; }

    private EntryKind(string name, string flatFile, string folder, string entryFile)
    {
        Name = name;
        FlatFile = flatFile;
        Folder = folder;
        EntryFile = entryFile;
    }
}
