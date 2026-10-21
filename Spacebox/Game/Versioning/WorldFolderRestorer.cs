using Engine;
using Engine.Utils;

namespace Spacebox.Game.Versioning;

public sealed class WorldFolderRestorer
{
    public const string ArchiveRoot = "world";

    private readonly Action<string, string> _moveDirectory;

    public WorldFolderRestorer(Action<string, string> moveDirectory = null)
    {
        _moveDirectory = moveDirectory ?? Directory.Move;
    }

    public string Restore(string backup, string worldFolder)
    {
        if (backup == null) return null;
        if (!File.Exists(backup)) return "the backup file is missing";

        string target = worldFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string suffix = Guid.NewGuid().ToString("N");
        string temp = target + "_restore_" + suffix;
        string aside = target + "_old_" + suffix;

        try
        {
            DirectoryArchive.Extract(File.ReadAllBytes(backup), temp);
            string restored = Path.Combine(temp, ArchiveRoot);
            if (!Directory.Exists(restored)) throw new InvalidDataException("the backup has no world folder");

            bool hadWorld = Directory.Exists(target);
            if (hadWorld) _moveDirectory(target, aside);

            try
            {
                _moveDirectory(restored, target);
            }
            catch
            {
                if (hadWorld) RollBack(aside, target);
                throw;
            }

            if (hadWorld) TryDelete(aside);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private void RollBack(string aside, string target)
    {
        try
        {
            if (Directory.Exists(target)) Directory.Delete(target, true);
            _moveDirectory(aside, target);
        }
        catch (Exception ex)
        {
            Debug.Error($"[WorldUpgrader] Could not move the world back from {aside}: {ex.Message}");
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
            Debug.Warning($"[WorldUpgrader] Could not delete {folder}: {ex.Message}");
        }
    }
}
