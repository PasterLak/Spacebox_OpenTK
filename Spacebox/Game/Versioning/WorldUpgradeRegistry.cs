using Spacebox.Game.Versioning.Steps;

namespace Spacebox.Game.Versioning;

public enum WorldUpgradeStatus
{
    Ok,
    UpToDate,
    TooOld,
    Newer,
    NoPath,
    InvalidVersion,
    UnsupportedFormat,
    Failed
}

public sealed class WorldUpgradeRegistry
{
    public const string DefaultMinimumSupportedVersion = "0.1.2";

    private readonly Dictionary<string, IWorldUpgradeStep> _byFrom = new Dictionary<string, IWorldUpgradeStep>(StringComparer.Ordinal);

    public WorldUpgradeRegistry(string minimumSupportedVersion = null)
    {
        MinimumSupportedVersion = minimumSupportedVersion;
    }

    public string MinimumSupportedVersion { get; }

    public static WorldUpgradeRegistry CreateDefault()
    {
        var registry = new WorldUpgradeRegistry(DefaultMinimumSupportedVersion);
        registry.Register(new Upgrade_0_1_2_To_0_1_5());
        return registry;
    }

    public IEnumerable<IWorldUpgradeStep> Steps => _byFrom.Values;

    public void Register(IWorldUpgradeStep step)
    {
        if (step == null) throw new ArgumentNullException(nameof(step));
        if (!Version.TryParse(step.FromVersion, out var from) || !Version.TryParse(step.ToVersion, out var to) || to <= from)
            throw new ArgumentException($"Upgrade step {step.FromVersion} -> {step.ToVersion} must go to a newer version");
        if (_byFrom.ContainsKey(step.FromVersion))
            throw new ArgumentException($"There is already an upgrade step from {step.FromVersion}");

        _byFrom[step.FromVersion] = step;
    }

    public WorldUpgradeStatus Check(string fromVersion, string toVersion, List<IWorldUpgradeStep> path, out string error)
    {
        path.Clear();
        error = null;

        if (!Version.TryParse(fromVersion, out var current) || !Version.TryParse(toVersion, out var target))
        {
            error = $"Unknown version '{fromVersion}' or '{toVersion}'";
            return WorldUpgradeStatus.InvalidVersion;
        }

        if (current == target) return WorldUpgradeStatus.UpToDate;

        if (current > target)
        {
            error = $"The world is from a newer game version ({fromVersion}) than this game ({toVersion})";
            return WorldUpgradeStatus.Newer;
        }

        if (MinimumSupportedVersion != null && Version.TryParse(MinimumSupportedVersion, out var minimum) && current < minimum)
        {
            error = $"Made with {fromVersion}, the oldest version that can be converted is {MinimumSupportedVersion}";
            return WorldUpgradeStatus.TooOld;
        }

        string version = fromVersion;
        while (version != toVersion)
        {
            if (!_byFrom.TryGetValue(version, out var step) || Version.Parse(step.ToVersion) > target)
            {
                error = $"No upgrade step from version {version} to {toVersion}";
                path.Clear();
                return WorldUpgradeStatus.NoPath;
            }

            path.Add(step);
            version = step.ToVersion;
        }

        return WorldUpgradeStatus.Ok;
    }

    public bool TryBuildPath(string fromVersion, string toVersion, List<IWorldUpgradeStep> path, out string error)
    {
        var status = Check(fromVersion, toVersion, path, out error);
        return status == WorldUpgradeStatus.Ok || status == WorldUpgradeStatus.UpToDate;
    }
}
