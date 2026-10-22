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

    public GameSetProject(string rootPath, string modId)
    {
        RootPath = rootPath;
        FolderName = Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        ModId = modId;
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

        file = EditorJsonFile.Load(GetFullPath(key), RootPath);
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
        foreach (var path in _pendingDeletes)
        {
            if (File.Exists(path)) File.Delete(path);
        }
        _pendingDeletes.Clear();

        foreach (var file in _files.Values)
        {
            if (file.IsDirty) file.Save();
        }
        _structureVersion = EditorJsonFile.NextStamp();
    }

    public void ReloadFromDisk()
    {
        _files.Clear();
        _pendingDeletes.Clear();
        _structureVersion = EditorJsonFile.NextStamp();
    }

    public void DiscardChanges()
    {
        _pendingDeletes.Clear();
        foreach (var key in _files.Keys.ToArray())
        {
            var file = _files[key];
            if (!file.IsDirty) continue;
            if (File.Exists(file.FullPath)) file.Reload();
            else _files.Remove(key);
        }
        _structureVersion = EditorJsonFile.NextStamp();
    }

    public static string Normalize(string relativePath)
    {
        return relativePath.Replace('\\', '/').TrimStart('/');
    }

    private static bool IsInFolder(string relativePath, string folder)
    {
        var directory = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? "";
        return string.Equals(directory, Normalize(folder).TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}
