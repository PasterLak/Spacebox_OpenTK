using Engine;

namespace Spacebox.Game.Generation.Tools;

public static class EntityFiles
{
    public const string Extension = ".entity";

    public static string NameOf(string fileNameOrName)
    {
        if (string.IsNullOrEmpty(fileNameOrName)) return fileNameOrName;

        string name = Path.GetFileName(fileNameOrName);
        while (name.Length > Extension.Length && name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - Extension.Length);

        return name;
    }

    public static string FileName(string name)
    {
        return NameOf(name) + Extension;
    }

    public static string PathOf(string folder, string name)
    {
        return Path.Combine(folder, FileName(name));
    }

    public static void RepairDoubleExtensions(string folder)
    {
        foreach (var file in Directory.GetFiles(folder, "*" + Extension + Extension))
        {
            try
            {
                string target = PathOf(folder, Path.GetFileName(file));

                if (!File.Exists(target) || File.GetLastWriteTimeUtc(file) >= File.GetLastWriteTimeUtc(target))
                {
                    File.Move(file, target, true);
                    Debug.Warning($"[EntityFiles] Restored newer save {Path.GetFileName(file)} -> {Path.GetFileName(target)}");
                }
                else
                {
                    File.Delete(file);
                    Debug.Warning($"[EntityFiles] Removed outdated duplicate {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[EntityFiles] Failed to repair {file}: {ex.Message}");
            }
        }
    }
}
