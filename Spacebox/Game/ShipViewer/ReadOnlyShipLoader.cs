using System.Text.Json;
using SharpNBT;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.ShipViewer;

public sealed class ReadOnlyShipLoader
{
    private const string SandboxWorldFolder = "world";
    private const string SandboxBackupFolder = "backup";

    private readonly WorldUpgradeRegistry _registry;
    private readonly string _targetVersion;
    private readonly string _scratchDirectory;

    public ReadOnlyShipLoader(WorldUpgradeRegistry registry, string targetVersion, string scratchDirectory)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _targetVersion = targetVersion;
        _scratchDirectory = scratchDirectory;
    }

    public static string DefaultScratchDirectory => Path.Combine(Path.GetTempPath(), "Spacebox", "ShipViewer");

    public CompoundTag Load(SavedWorld world, SavedShip ship)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (ship == null) throw new ArgumentNullException(nameof(ship));

        return world.Status switch
        {
            SavedWorldStatus.Ready => NbtFileEditor.Read(ship.FilePath),
            SavedWorldStatus.NeedsUpgrade => LoadUpgraded(world, ship),
            _ => throw new InvalidOperationException(world.Message)
        };
    }

    private CompoundTag LoadUpgraded(SavedWorld world, SavedShip ship)
    {
        string sandbox = Path.Combine(_scratchDirectory, Guid.NewGuid().ToString("N"));
        string worldCopy = Path.Combine(sandbox, SandboxWorldFolder);

        try
        {
            string entityCopy = Path.Combine(worldCopy, Path.GetRelativePath(world.FolderPath, ship.FilePath));
            Directory.CreateDirectory(Path.GetDirectoryName(entityCopy));
            File.Copy(ship.FilePath, entityCopy);

            foreach (var name in new[] { WorldFileLayout.WorldFile, WorldFileLayout.LegacyWorldFile })
            {
                string source = Path.Combine(world.FolderPath, name);
                if (File.Exists(source)) File.Copy(source, Path.Combine(worldCopy, name));
            }

            if (!WorldInfoFiles.TryRead(worldCopy, out var info)) throw new InvalidDataException("The world file is empty or damaged");
            var result = new WorldUpgrader(_registry, Path.Combine(sandbox, SandboxBackupFolder)).Upgrade(info, worldCopy, _targetVersion);
            if (!result.Success) throw new InvalidDataException(result.Error ?? $"The world could not be converted from {result.FromVersion}");

            return NbtFileEditor.Read(File.Exists(entityCopy) ? entityCopy : EntityFiles.PathInWorld(worldCopy, EntityFiles.NameOf(entityCopy)));
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    private static void TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        catch (Exception ex)
        {
            Engine.Debug.Warning($"[ShipViewer] Could not delete the temporary folder {folder}: {ex.Message}");
        }
    }
}
