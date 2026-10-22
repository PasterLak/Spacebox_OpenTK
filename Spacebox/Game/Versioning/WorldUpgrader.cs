using Spacebox.Game.GUI;

namespace Spacebox.Game.Versioning;

public sealed class WorldUpgradeResult
{
    public WorldUpgradeStatus Status { get; init; }
    public bool Success => Status == WorldUpgradeStatus.Ok || Status == WorldUpgradeStatus.UpToDate;
    public string FromVersion { get; init; } = "";
    public string ToVersion { get; init; } = "";
    public string BackupPath { get; init; }
    public string Error { get; init; }
    public string Summary { get; init; } = "";
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Log { get; init; } = Array.Empty<string>();
}

public sealed class WorldUpgrader
{
    public const string BackupFolderName = WorldBackup.FolderName;

    private readonly WorldUpgradeRegistry _registry;
    private readonly string _backupDirectory;
    private readonly Func<DateTime> _clock;
    private readonly List<IWorldUpgradeStep> _path = new List<IWorldUpgradeStep>();
    private readonly WorldFolderRestorer _restorer;

    public WorldUpgrader(WorldUpgradeRegistry registry, string backupDirectory, Func<DateTime> clock = null, WorldFolderRestorer restorer = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _backupDirectory = backupDirectory;
        _clock = clock ?? (() => DateTime.Now);
        _restorer = restorer ?? new WorldFolderRestorer();
    }

    public WorldUpgradeStatus Check(string fromVersion, string toVersion, out string error)
    {
        return _registry.Check(fromVersion, toVersion, _path, out error);
    }

    public WorldUpgradeStatus Check(WorldInfo info, string worldFolder, string targetVersion, out string error)
    {
        var status = _registry.Check(info?.GameVersion ?? "", targetVersion, _path, out error);
        if (status != WorldUpgradeStatus.Ok) return status;

        error = CheckFirstStep(new WorldUpgradeContext(info, worldFolder));
        return error == null ? WorldUpgradeStatus.Ok : WorldUpgradeStatus.UnsupportedFormat;
    }

    public bool CanUpgrade(string fromVersion, string toVersion, out string error)
    {
        return _registry.TryBuildPath(fromVersion, toVersion, _path, out error);
    }

    public IReadOnlyList<IWorldUpgradeStep> GetPath(string fromVersion, string toVersion)
    {
        return _registry.Check(fromVersion, toVersion, _path, out _) == WorldUpgradeStatus.Ok ? _path.ToArray() : Array.Empty<IWorldUpgradeStep>();
    }

    public WorldUpgradeResult Upgrade(WorldInfo info, string worldFolder, string targetVersion, GameSetIdIndex ids = null)
    {
        string from = info?.GameVersion ?? "";
        if (info == null) return Fail(WorldUpgradeStatus.Failed, from, targetVersion, "No world", null, null);

        var status = _registry.Check(from, targetVersion, _path, out var error);
        if (status == WorldUpgradeStatus.UpToDate) return new WorldUpgradeResult { Status = status, FromVersion = from, ToVersion = targetVersion, Summary = "already up to date" };
        if (status != WorldUpgradeStatus.Ok) return Fail(status, from, targetVersion, error, null, null);

        var context = new WorldUpgradeContext(info, worldFolder);
        string unsupported = CheckFirstStep(context);
        if (unsupported != null) return Fail(WorldUpgradeStatus.UnsupportedFormat, from, targetVersion, unsupported, null, null);

        string backup = null;
        if (context.HasFolder)
        {
            try
            {
                backup = CreateBackup(worldFolder, from);
                context.Write($"Backup: {backup}");
            }
            catch (Exception ex)
            {
                return Fail(WorldUpgradeStatus.Failed, from, targetVersion, $"Could not create a backup: {ex.Message}", null, context.Log);
            }
        }

        var steps = _path.ToArray();
        foreach (var step in steps)
        {
            try
            {
                step.Apply(context);
                info.GameVersion = step.ToVersion;
                context.Write(step.Description);
            }
            catch (Exception ex)
            {
                info.GameVersion = from;
                string restoreError = _restorer.Restore(backup, worldFolder);
                string message = $"Upgrade {step.Description} failed: {ex.Message}" + (restoreError == null ? "" : $". Restoring the backup failed: {restoreError}");
                return Fail(WorldUpgradeStatus.Failed, from, targetVersion, message, backup, context.Log);
            }
        }

        var warnings = context.HasFolder && ids != null ? WorldValidator.FindUnknownIds(worldFolder, ids) : new List<string>();
        return new WorldUpgradeResult
        {
            Status = WorldUpgradeStatus.Ok,
            FromVersion = from,
            ToVersion = info.GameVersion,
            BackupPath = backup,
            Summary = context.Summary(),
            Warnings = warnings,
            Log = context.Log
        };
    }

    private string CheckFirstStep(WorldUpgradeContext context)
    {
        if (_path.Count == 0 || !context.HasFolder) return null;

        try
        {
            return _path[0].Check(context);
        }
        catch (Exception ex)
        {
            return $"The world files could not be read: {ex.Message}";
        }
    }

    private string CreateBackup(string worldFolder, string version)
    {
        return WorldBackup.Create(worldFolder, version, _backupDirectory, _clock());
    }

    private static WorldUpgradeResult Fail(WorldUpgradeStatus status, string from, string to, string error, string backup, IReadOnlyList<string> log)
    {
        return new WorldUpgradeResult { Status = status, FromVersion = from, ToVersion = to, Error = error, BackupPath = backup, Log = log ?? Array.Empty<string>() };
    }
}
