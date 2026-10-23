namespace Spacebox.Game.Resource.Content;

public sealed class GameSetStack
{
    public InstalledGameSet Base { get; }
    public IReadOnlyList<InstalledGameSet> Extensions { get; }
    public IReadOnlyList<InstalledGameSet> Fallbacks { get; }
    public ContentLayer Fallback { get; }
    public IReadOnlyList<ContentLayer> Layers { get; }

    public GameSetStack(InstalledGameSet baseSet, IReadOnlyList<InstalledGameSet> extensions, IReadOnlyList<InstalledGameSet> fallbacks)
    {
        Base = baseSet;
        Extensions = extensions ?? Array.Empty<InstalledGameSet>();
        Fallbacks = fallbacks ?? Array.Empty<InstalledGameSet>();

        var layers = new List<ContentLayer> { baseSet.ToLayer() };
        foreach (var extension in Extensions)
        {
            layers.Add(extension.ToLayer());
            string adapter = GameSetFormat.AdapterFolder(extension.Folder, baseSet);
            if (adapter != null) layers.Add(new ContentLayer(extension.Id, adapter));
        }
        Layers = layers;

        if (Fallbacks.Count > 0) Fallback = Fallbacks[0].ToLayer();
    }

    public IReadOnlyList<ContentLayer> AssetLayers
    {
        get
        {
            var layers = new List<ContentLayer>(Layers.Count + Fallbacks.Count);
            for (int i = Fallbacks.Count - 1; i >= 0; i--) layers.Add(Fallbacks[i].ToLayer());
            layers.AddRange(Layers);
            return layers;
        }
    }

    public IReadOnlyList<string> FallbackFolders
    {
        get
        {
            var folders = new List<string>(Fallbacks.Count);
            foreach (var fallback in Fallbacks) folders.Add(fallback.Folder);
            return folders;
        }
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
        for (int i = Extensions.Count - 1; i >= 0; i--)
        {
            string adapter = GameSetFormat.AdapterFolder(Extensions[i].Folder, Base);
            if (adapter != null) roots.Add(adapter);
            roots.Add(Extensions[i].Folder);
        }
        roots.Add(Base.Folder);
        foreach (var fallback in Fallbacks) roots.Add(fallback.Folder);
        return roots.ToArray();
    }

    public GameSetContent ReadContent() => GameSetContentReader.Read(Layers, Fallback);

    public static bool SameFolder(string a, string b)
    {
        return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
