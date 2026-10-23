using System.Text;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipFileNames
{
    public const int MaxIdLength = 40;
    public const string FolderName = "Ships";

    public static string Folder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderName);

    public static string MakeId(string name)
    {
        var builder = new StringBuilder();
        foreach (char c in (name ?? "").Trim().ToLowerInvariant())
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) builder.Append(c);
            else if (builder.Length > 0 && builder[^1] != '_') builder.Append('_');
            if (builder.Length >= MaxIdLength) break;
        }

        string id = builder.ToString().Trim('_');
        return id.Length == 0 ? "ship" : id;
    }

    public static string UniquePath(string folder, string id, Func<string, bool> exists = null)
    {
        exists ??= File.Exists;
        string path = Path.Combine(folder, id + ShipFile.Extension);
        for (int i = 2; exists(path); i++) path = Path.Combine(folder, $"{id}_{i}{ShipFile.Extension}");
        return path;
    }
}
