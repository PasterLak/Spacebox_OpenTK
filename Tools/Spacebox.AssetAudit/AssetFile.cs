namespace Spacebox.AssetAudit;

public sealed record AssetFile(string RelativePath, string FullPath, long Size)
{
    public string Extension => Path.GetExtension(RelativePath).ToLowerInvariant();
}

public enum AssetVerdict
{
    Undecided,
    Keep,
    Drop
}

public interface IAssetUsageRule
{
    AssetVerdict Evaluate(AssetFile file);
}
