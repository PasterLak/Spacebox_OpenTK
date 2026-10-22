namespace Spacebox.AssetAudit;

public sealed class AssetAuditResult
{
    public List<AssetFile> Used { get; } = new List<AssetFile>();
    public List<AssetFile> Unused { get; } = new List<AssetFile>();
    public List<string> MissingReferences { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();

    public long UnusedBytes => Unused.Sum(file => file.Size);
}

public sealed class AssetAuditor
{
    public const string GameFolder = "Spacebox";
    public const string KeepListFile = "Spacebox/Build/asset-keep.txt";
    public const string ResourcesFolder = "Resources";
    public const string ModsFolder = "Mods";
    public static readonly string[] GameSetFolders = { "Mods/GameSets", "Mods/Extensions" };

    private static readonly string[] CodeFolders = { "Spacebox", "Engine" };
    private static readonly string[] SkippedFolders = { "bin", "obj", ".vs", "Build" };

    private readonly string _repoRoot;

    public AssetAuditor(string repoRoot)
    {
        _repoRoot = Path.GetFullPath(repoRoot);
    }

    public AssetAuditResult Analyze()
    {
        var result = new AssetAuditResult();
        string gameRoot = Path.Combine(_repoRoot, GameFolder);
        var files = CollectAssets(gameRoot);
        var index = new ReferenceIndex();
        var codePaths = new List<string>();

        IndexCode(index, codePaths);
        IndexGameSets(gameRoot, index, result);

        var shaders = files.Where(ShaderRule.IsShader).ToList();
        var usedShaders = ShaderIncludeGraph.CollectUsed(shaders, file => index.IsReferenced(file.RelativePath));

        var rules = new IAssetUsageRule[]
        {
            new KeepListRule(LoadKeepList()),
            new AlwaysKeepRule(),
            new IgnoredFolderRule(),
            new ShaderRule(usedShaders),
            new ReferenceRule(index)
        };

        foreach (var file in files)
        {
            if (Classify(file, rules) == AssetVerdict.Drop) result.Unused.Add(file);
            else result.Used.Add(file);
        }

        FindMissingReferences(files, codePaths, result);
        return result;
    }

    private static AssetVerdict Classify(AssetFile file, IAssetUsageRule[] rules)
    {
        foreach (var rule in rules)
        {
            var verdict = rule.Evaluate(file);
            if (verdict != AssetVerdict.Undecided) return verdict;
        }
        return AssetVerdict.Keep;
    }

    private static List<AssetFile> CollectAssets(string gameRoot)
    {
        var files = new List<AssetFile>();
        foreach (var folder in new[] { ResourcesFolder, ModsFolder })
        {
            string root = Path.Combine(gameRoot, folder);
            if (!Directory.Exists(root)) continue;

            foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(gameRoot, path).Replace('\\', '/');
                files.Add(new AssetFile(relative, path, new FileInfo(path).Length));
            }
        }
        files.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        return files;
    }

    private void IndexCode(ReferenceIndex index, List<string> codePaths)
    {
        foreach (var folder in CodeFolders)
        {
            string root = Path.Combine(_repoRoot, folder);
            if (!Directory.Exists(root)) continue;

            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (IsSkipped(root, file)) continue;

                foreach (var literal in CSharpLiteralScanner.Scan(File.ReadAllText(file)))
                {
                    index.AddLiteral(literal.Text, literal.IsInterpolated);
                    if (!literal.IsInterpolated && IsAppAssetPath(literal.Text)) codePaths.Add(AssetPath.Normalize(literal.Text));
                }
            }
        }
    }

    private static void IndexGameSets(string gameRoot, ReferenceIndex index, AssetAuditResult result)
    {
        foreach (var folder in GameSetFolders) IndexGameSets(gameRoot, folder, index, result);
    }

    private static void IndexGameSets(string gameRoot, string folder, ReferenceIndex index, AssetAuditResult result)
    {
        string root = Path.Combine(gameRoot, folder);
        if (!Directory.Exists(root)) return;

        foreach (var gameSet in Directory.EnumerateDirectories(root))
        {
            string prefix = folder + "/" + Path.GetFileName(gameSet) + "/";
            foreach (var json in Directory.EnumerateFiles(gameSet, "*.json", SearchOption.AllDirectories))
            {
                var values = JsonStringCollector.Collect(File.ReadAllText(json), out var error);
                if (error != null) result.Warnings.Add($"{Path.GetRelativePath(gameRoot, json)}: {error}");

                foreach (var value in values)
                {
                    index.AddLiteral(value, false);
                    if (value.Contains('/') || value.Contains('\\')) index.AddPath(prefix + value);
                }
            }
        }
    }

    private void FindMissingReferences(List<AssetFile> files, List<string> codePaths, AssetAuditResult result)
    {
        var existing = new HashSet<string>(files.Select(file => AssetPath.WithoutExtension(file.RelativePath)), StringComparer.OrdinalIgnoreCase);
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in codePaths)
        {
            string withoutExtension = AssetPath.WithoutExtension(path);
            if (existing.Contains(withoutExtension) || Directory.Exists(Path.Combine(_repoRoot, GameFolder, path))) continue;
            if (reported.Add(withoutExtension)) result.MissingReferences.Add(path);
        }
    }

    private List<GlobPattern> LoadKeepList()
    {
        string path = Path.Combine(_repoRoot, KeepListFile);
        return File.Exists(path) ? GlobPattern.ParseLines(File.ReadAllLines(path)) : new List<GlobPattern>();
    }

    private static bool IsAppAssetPath(string literal)
    {
        string normalized = AssetPath.Normalize(literal);
        if (!normalized.StartsWith(ResourcesFolder + "/", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalized.IndexOfAny(new[] { '{', ' ', '\n', '*' }) >= 0) return false;

        string extension = Path.GetExtension(normalized).ToLowerInvariant();
        return extension.Length == 0 || Array.IndexOf(AlwaysKeepRule.AuditedExtensions, extension) >= 0;
    }

    private static bool IsSkipped(string root, string file)
    {
        foreach (var segment in Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (Array.Exists(SkippedFolders, skipped => skipped.Equals(segment, StringComparison.OrdinalIgnoreCase))) return true;
        }
        return false;
    }
}
