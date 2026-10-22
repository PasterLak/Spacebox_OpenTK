namespace Spacebox.Tests.GameSets;

public sealed class TempGameSets : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "spacebox_gamesets_" + Guid.NewGuid().ToString("N"));

    public TempGameSets()
    {
        Directory.CreateDirectory(Root);
    }

    public string Folder(string name)
    {
        string path = Path.Combine(Root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public string Write(string relative, string content)
    {
        string path = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public string Config(string folder, string id, string extends = "", params string[] includes)
    {
        string includeList = string.Join(", ", includes.Select(i => $"\"{i}\""));
        return Write($"{folder}/config.json", $"{{ \"ModId\": \"{id}\", \"ModName\": \"{id}\", \"Extends\": \"{extends}\", \"Includes\": [ {includeList} ] }}");
    }

    public void Dispose()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}
