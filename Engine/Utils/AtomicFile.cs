namespace Engine.Utils;

public static class AtomicFile
{
    public const string TempSuffix = ".tmp";

    public static void WriteAllText(string path, string contents)
    {
        Write(path, temp => File.WriteAllText(temp, contents));
    }

    public static void Write(string path, Action<string> writeTo)
    {
        string temp = path + TempSuffix;
        try
        {
            writeTo(temp);
            File.Move(temp, path, true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
