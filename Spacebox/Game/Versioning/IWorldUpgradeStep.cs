namespace Spacebox.Game.Versioning;

public interface IWorldUpgradeStep
{
    string FromVersion { get; }
    string ToVersion { get; }
    string Description { get; }

    string Check(WorldUpgradeContext context);
    void Apply(WorldUpgradeContext context);
}

public abstract class WorldUpgradeStep : IWorldUpgradeStep
{
    protected WorldUpgradeStep(string fromVersion, string toVersion)
    {
        FromVersion = fromVersion;
        ToVersion = toVersion;
    }

    public string FromVersion { get; }
    public string ToVersion { get; }
    public virtual string Description => $"{FromVersion} -> {ToVersion}";

    public virtual string Check(WorldUpgradeContext context) => null;

    public abstract void Apply(WorldUpgradeContext context);
}

public sealed class VersionBumpStep : WorldUpgradeStep
{
    public VersionBumpStep(string fromVersion, string toVersion) : base(fromVersion, toVersion)
    {
    }

    public override string Description => $"{FromVersion} -> {ToVersion}: no file changes";

    public override void Apply(WorldUpgradeContext context)
    {
    }
}
