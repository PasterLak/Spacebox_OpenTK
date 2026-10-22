namespace Spacebox.Network.Dashboard;

public static class PrivateFile
{
    public static void WriteAllText(string path, string text)
    {
        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(path, text);
            return;
        }

        using (File.Create(path)) { }
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.WriteAllText(path, text);
    }
}
