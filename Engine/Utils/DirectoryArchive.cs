using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Engine.Utils;

public readonly record struct ArchiveFolder(string Path, string EntryName);

public static class DirectoryArchive
{
    public static byte[] Pack(IReadOnlyList<ArchiveFolder> folders)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var folder in folders)
            {
                foreach (var (file, relative) in EnumerateFiles(folder.Path))
                    archive.CreateEntryFromFile(file, folder.EntryName + "/" + relative, CompressionLevel.Optimal);
            }
        }

        return stream.ToArray();
    }

    public static string ComputeHash(IReadOnlyList<ArchiveFolder> folders)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> lengthBytes = stackalloc byte[8];

        foreach (var folder in folders)
        {
            foreach (var (file, relative) in EnumerateFiles(folder.Path))
            {
                hash.AppendData(Encoding.UTF8.GetBytes(folder.EntryName + "/" + relative + "\0"));

                byte[] content = File.ReadAllBytes(file);
                BitConverter.TryWriteBytes(lengthBytes, (long)content.Length);
                hash.AppendData(lengthBytes);
                hash.AppendData(content);
            }
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    public const long DefaultMaxExtractedBytes = 2L * 1024 * 1024 * 1024;
    public const int DefaultMaxEntries = 100000;

    private static StringComparison PathComparison => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static void Extract(byte[] data, string targetDirectory, long maxExtractedBytes = DefaultMaxExtractedBytes, int maxEntries = DefaultMaxEntries)
    {
        string root = Path.GetFullPath(targetDirectory);
        string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        if (archive.Entries.Count > maxEntries)
            throw new InvalidDataException($"The archive has {archive.Entries.Count} entries, more than the allowed {maxEntries}");

        long total = 0;
        foreach (var entry in archive.Entries)
        {
            total += entry.Length;
            if (entry.Length < 0 || total > maxExtractedBytes)
                throw new InvalidDataException($"The archive unpacks to more than {maxExtractedBytes / (1024 * 1024)} MB");

            if (string.IsNullOrEmpty(entry.Name)) continue;
            string destination = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!destination.StartsWith(rootWithSeparator, PathComparison))
                throw new InvalidDataException($"Archive entry '{entry.FullName}' is outside the target folder");
        }

        Directory.CreateDirectory(root);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            string destination = Path.GetFullPath(Path.Combine(root, entry.FullName));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            entry.ExtractToFile(destination, true);
        }
    }

    private static List<(string File, string Relative)> EnumerateFiles(string folder)
    {
        var files = new List<(string, string)>();
        if (!Directory.Exists(folder)) return files;

        foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            files.Add((file, Path.GetRelativePath(folder, file).Replace('\\', '/')));

        files.Sort((a, b) => string.CompareOrdinal(a.Item2, b.Item2));
        return files;
    }
}
