namespace Spacebox.Game.Resource.Content;

public sealed class GameSetStack
{
    public InstalledGameSet Base { get; }
    public IReadOnlyList<InstalledGameSet> Extensions { get; }
    public ContentLayer Fallback { get; }
    public IReadOnlyList<ContentLayer> Layers { get; }

    public GameSetStack(InstalledGameSet baseSet, IReadOnlyList<InstalledGameSet> extensions, string defaultFolder)
    {
        Base = baseSet;
        Extensions = extensions ?? Array.Empty<InstalledGameSet>();

        var layers = new List<ContentLayer> { baseSet.ToLayer() };
        foreach (var extension in Extensions) layers.Add(extension.ToLayer());
        Layers = layers;

        if (!string.IsNullOrEmpty(defaultFolder) && Directory.Exists(defaultFolder) && !SameFolder(defaultFolder, baseSet.Folder))
            Fallback = new ContentLayer(Globals.GameSet.Default.ToLowerInvariant(), defaultFolder);
    }

    public IReadOnlyList<string> ExtensionIds
    {
        get
        {
            var ids = new List<string>(Extensions.Count);
            foreach (var extension in Extensions) ids.Add(extension.Id);
            return ids;
        }
    }

    public IReadOnlyList<string> ExtensionFolders
    {
        get
        {
            var folders = new List<string>(Extensions.Count);
            foreach (var extension in Extensions) folders.Add(extension.Folder);
            return folders;
        }
    }

    public string[] SearchRootsNewestFirst()
    {
        var roots = new List<string>();
        for (int i = Extensions.Count - 1; i >= 0; i--) roots.Add(Extensions[i].Folder);
        roots.Add(Base.Folder);
        if (Fallback != null) roots.Add(Fallback.Root);
        return roots.ToArray();
    }

    public GameSetContent ReadContent() => GameSetContentReader.Read(Layers, Fallback);

    public static bool SameFolder(string a, string b)
    {
        return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
