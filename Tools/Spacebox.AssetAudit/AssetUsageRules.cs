namespace Spacebox.AssetAudit;

public sealed class KeepListRule : IAssetUsageRule
{
    private readonly IReadOnlyList<GlobPattern> _patterns;

    public KeepListRule(IReadOnlyList<GlobPattern> patterns)
    {
        _patterns = patterns;
    }

    public AssetVerdict Evaluate(AssetFile file)
    {
        foreach (var pattern in _patterns)
        {
            if (pattern.IsMatch(file.RelativePath)) return AssetVerdict.Keep;
        }
        return AssetVerdict.Undecided;
    }
}

public sealed class AlwaysKeepRule : IAssetUsageRule
{
    public static readonly string[] AuditedExtensions = { ".png", ".jpg", ".jpeg", ".ogg", ".wav", ".obj", ".glsl", ".fs", ".vs" };

    public static readonly string[] KeptFolders =
    {
        "Resources/Textures/UI/",
        "Resources/Audio/Mood/",
        "Resources/Dashboard/",
        "Resources/Fonts/"
    };

    public AssetVerdict Evaluate(AssetFile file)
    {
        if (Array.IndexOf(AuditedExtensions, file.Extension) < 0) return AssetVerdict.Keep;
        if (AssetPath.NameOf(file.RelativePath).Equals("icon", StringComparison.OrdinalIgnoreCase)) return AssetVerdict.Keep;

        foreach (var folder in KeptFolders)
        {
            if (file.RelativePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) return AssetVerdict.Keep;
        }
        return AssetVerdict.Undecided;
    }
}

public sealed class IgnoredFolderRule : IAssetUsageRule
{
    private const string GameSetsFolder = "GameSets/";

    public AssetVerdict Evaluate(AssetFile file)
    {
        if (!file.RelativePath.StartsWith(GameSetsFolder, StringComparison.OrdinalIgnoreCase)) return AssetVerdict.Undecided;

        string insideGameSet = file.RelativePath.Substring(GameSetsFolder.Length);
        return AssetPath.HasSegmentStartingWith(insideGameSet, '_') ? AssetVerdict.Drop : AssetVerdict.Undecided;
    }
}

public sealed class ShaderRule : IAssetUsageRule
{
    public const string ShadersFolder = "Resources/Shaders/";

    private readonly HashSet<string> _used;

    public ShaderRule(HashSet<string> used)
    {
        _used = used;
    }

    public static bool IsShader(AssetFile file) => file.RelativePath.StartsWith(ShadersFolder, StringComparison.OrdinalIgnoreCase);

    public AssetVerdict Evaluate(AssetFile file)
    {
        if (!IsShader(file)) return AssetVerdict.Undecided;
        return _used.Contains(file.RelativePath) ? AssetVerdict.Keep : AssetVerdict.Drop;
    }
}

public sealed class ReferenceRule : IAssetUsageRule
{
    private readonly ReferenceIndex _index;

    public ReferenceRule(ReferenceIndex index)
    {
        _index = index;
    }

    public AssetVerdict Evaluate(AssetFile file)
    {
        if (_index.IsReferenced(file.RelativePath)) return AssetVerdict.Keep;

        string appRelative = GameSetOverridePath(file.RelativePath);
        if (appRelative != null && _index.IsReferenced(appRelative)) return AssetVerdict.Keep;

        return AssetVerdict.Drop;
    }

    private static string GameSetOverridePath(string relativePath)
    {
        var segments = relativePath.Split('/');
        if (segments.Length < 4 || !segments[0].Equals("GameSets", StringComparison.OrdinalIgnoreCase)) return null;
        if (!segments[2].Equals("Resources", StringComparison.OrdinalIgnoreCase)) return null;
        return string.Join('/', segments, 2, segments.Length - 2);
    }
}
