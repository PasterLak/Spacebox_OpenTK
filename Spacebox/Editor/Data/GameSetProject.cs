using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public sealed class GameSetProject
{
    private readonly Dictionary<string, EditorJsonFile> _files = new Dictionary<string, EditorJsonFile>(StringComparer.OrdinalIgnoreCase);
    private readonly List<EditorJsonFile> _folderScratch = new List<EditorJsonFile>();
    private readonly HashSet<string> _pendingDeletes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private int _structureVersion;

    public string RootPath { get; }
    public string FolderName { get; }
    public string ModId { get; }
    public SourceFolderMirror Mirror { get; set; }
    public bool IsReadOnly { get; }

    public GameSetProject(string rootPath, string modId)
    {
        RootPath = rootPath;
        FolderName = Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        ModId = modId;
        IsReadOnly = Spacebox.Game.Resource.Content.GameSetRoles.IsEmpty(modId) || Spacebox.Game.Resource.Content.GameSetRoles.IsEmpty(FolderName);
    }

    public int Version
    {
        get
        {
            int version = _structureVersion;
            foreach (var file in _files.Values) version = Math.Max(version, file.Version);
            return version;
        }
    }

    public int DirtyCount
    {
        get
        {
            int count = _pendingDeletes.Count;
            foreach (var file in _files.Values)
            {
                if (file.IsDirty) count++;
            }
            return count;
        }
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(RootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public EditorJsonFile GetFile(string relativePath)
    {
        var key = Normalize(relativePath);
        if (_files.TryGetValue(key, out var file)) return file;

        var fullPath = GetFullPath(key);
        file = _pendingDeletes.Contains(fullPath) ? EditorJsonFile.Missing(fullPath, RootPath) : EditorJsonFile.Load(fullPath, RootPath);
        _files[key] = file;
        return file;
    }

    public IReadOnlyList<EditorJsonFile> GetFolderFiles(string relativeFolder)
    {
        _folderScratch.Clear();
        var folder = GetFullPath(relativeFolder);
        if (!Directory.Exists(folder)) return _folderScratch.ToArray();

        var paths = Directory.GetFiles(folder, "*.json");
        Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (_pendingDeletes.Contains(path)) continue;
            _folderScratch.Add(GetFile(Path.GetRelativePath(RootPath, path)));
        }

        foreach (var file in _files.Values)
        {
            if (!File.Exists(file.FullPath) && file.Root != null && IsInFolder(file.RelativePath, relativeFolder) && !_folderScratch.Contains(file))
                _folderScratch.Add(file);
        }

        return _folderScratch.ToArray();
    }

    public IReadOnlyList<string> ListJsonFiles()
    {
        var result = new List<string>();
        if (Directory.Exists(RootPath))
        {
            foreach (var path in Directory.GetFiles(RootPath, "*.json", SearchOption.AllDirectories))
            {
                if (!_pendingDeletes.Contains(path)) result.Add(Normalize(Path.GetRelativePath(RootPath, path)));
            }
        }

        foreach (var file in _files.Values)
        {
            if (file.Root != null && !result.Contains(file.RelativePath)) result.Add(file.RelativePath);
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public bool Exists(string relativePath)
    {
        var key = Normalize(relativePath);
        var fullPath = GetFullPath(key);
        return File.Exists(fullPath) && !_pendingDeletes.Contains(fullPath) || _files.TryGetValue(key, out var file) && file.Root != null;
    }

    public EditorJsonFile CreateFile(string relativePath, JsonNode root)
    {
        var key = Normalize(relativePath);
        if (!IsInsideRoot(GetFullPath(key))) throw new ArgumentException($"'{relativePath}' is outside the GameSet folder", nameof(relativePath));
        _pendingDeletes.Remove(GetFullPath(key));
        var file = EditorJsonFile.Create(GetFullPath(key), RootPath, root);
        _files[key] = file;
        _structureVersion = EditorJsonFile.NextStamp();
        return file;
    }

    public void DeleteFile(EditorJsonFile file)
    {
        if (file == null) return;
        if (File.Exists(file.FullPath)) _pendingDeletes.Add(file.FullPath);
        _files.Remove(file.RelativePath);
        _structureVersion = EditorJsonFile.NextStamp();
    }

    public void SaveAll()
    {
        if (IsReadOnly) throw new InvalidOperationException(ReadOnlyMessage);
        var failures = new List<string>();
        foreach (var path in _pendingDeletes.ToArray())
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                Mirror?.Delete(Normalize(Path.GetRelativePath(RootPath, path)));
                _pendingDeletes.Remove(path);
            }
            catch (Exception ex)
            {
                failures.Add($"{Normalize(Path.GetRelativePath(RootPath, path))}: {ex.Message}");
            }
        }

        foreach (var file in _files.Values)
        {
            if (!file.IsDirty || file.Error != null) continue;
            try
            {
                file.Save();
                if (file.Root != null) Mirror?.Write(file.RelativePath, EditorJsonFile.Serialize(file.Root));
            }
            catch (Exception ex)
            {
                failures.Add($"{file.RelativePath}: {ex.Message}");
            }
        }
        _structureVersion = EditorJsonFile.NextStamp();
        if (failures.Count > 0) throw new IOException($"{failures.Count} file(s) could not be saved: {string.Join("; ", failures)}");
    }

    public void ReloadFromDisk()
    {
        _pendingDeletes.Clear();
        foreach (var key in _files.Keys.ToArray())
        {
            var file = _files[key];
            if (File.Exists(file.FullPath)) file.Reload();
            else _files.Remove(key);
        }
        _structureVersion = EditorJsonFile.NextStamp();
    }

    public void DiscardChanges()
    {
        ForgetPendingDeletes();
        foreach (var key in _files.Keys.ToArray())
        {
            var file = _files[key];
            if (!file.IsDirty) continue;
            if (File.Exists(file.FullPath)) file.Reload();
            else _files.Remove(key);
        }
        _structureVersion = EditorJsonFile.NextStamp();
    }

    private void ForgetPendingDeletes()
    {
        foreach (var key in _files.Keys.ToArray())
        {
            var file = _files[key];
            if (!file.IsDirty && _pendingDeletes.Contains(file.FullPath)) _files.Remove(key);
        }
        _pendingDeletes.Clear();
    }

    public const string ReadOnlyMessage = "Empty is read-only. Create a new GameSet in Mods to build on it.";

    public static bool IsValidNewFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return false;
        if (name.Contains("..") || name.Contains(':') || name.StartsWith('/') || name.StartsWith('\\')) return false;
        return !Path.IsPathRooted(name) && name.IndexOfAny(Path.GetInvalidPathChars()) < 0;
    }

    public static string Normalize(string relativePath)
    {
        return relativePath.Replace('\\', '/').TrimStart('/');
    }

    private bool IsInsideRoot(string fullPath)
    {
        string root = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(fullPath).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInFolder(string relativePath, string folder)
    {
        var directory = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? "";
        return string.Equals(directory, Normalize(folder).TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}
