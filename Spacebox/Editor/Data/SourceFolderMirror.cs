namespace Spacebox.Editor.Data;

public sealed class SourceFolderMirror
{
    public SourceFolderMirror(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public static SourceFolderMirror TryCreate(string installedFolder)
    {
        string source = Spacebox.Game.Resource.DevelopmentSources.FindSourceFolder(installedFolder);
        return source == null ? null : new SourceFolderMirror(source);
    }

    public void Write(string relativePath, string content)
    {
        string path = FullPath(relativePath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            Engine.Debug.Warning($"[Editor] Could not write the source copy {path}: {ex.Message}");
        }
    }

    public void Delete(string relativePath)
    {
        string path = FullPath(relativePath);
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            Engine.Debug.Warning($"[Editor] Could not delete the source copy {path}: {ex.Message}");
        }
    }

    private string FullPath(string relativePath)
    {
        return Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
