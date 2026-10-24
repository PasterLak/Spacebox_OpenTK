using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Spacebox.Game.Resource;

public static class ModArchive
{
    private enum Kind
    {
        Zip,
        Archive,
        TarStream
    }

    private static readonly (string Suffix, Kind Kind)[] Formats =
    {
        (".tar.gz", Kind.TarStream),
        (".tar.bz2", Kind.TarStream),
        (".tar.xz", Kind.TarStream),
        (".tar.zst", Kind.TarStream),
        (".tgz", Kind.TarStream),
        (".tbz2", Kind.TarStream),
        (".txz", Kind.TarStream),
        (".tar", Kind.TarStream),
        (".zip", Kind.Zip),
        (".rar", Kind.Archive),
        (".7z", Kind.Archive)
    };

    public static bool IsArchive(string path) => Match(path) >= 0;

    public static string NameWithoutExtension(string path)
    {
        string name = Path.GetFileName(path);
        int index = Match(path);
        return index < 0 ? Path.GetFileNameWithoutExtension(path) : name.Substring(0, name.Length - Formats[index].Suffix.Length);
    }

    public static void Extract(string path, string destination)
    {
        int index = Match(path);
        if (index < 0) throw new NotSupportedException($"'{Path.GetFileName(path)}' is not a supported mod archive");

        Directory.CreateDirectory(destination);
        switch (Formats[index].Kind)
        {
            case Kind.Zip:
                ZipFile.ExtractToDirectory(path, destination);
                break;
            case Kind.Archive:
                ExtractArchive(path, destination);
                break;
            default:
                ExtractTar(path, destination);
                break;
        }
    }

    private static void ExtractArchive(string path, string destination)
    {
        using var archive = ArchiveFactory.Open(path);
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory) continue;
            string target = PrepareTarget(destination, entry.Key);
            entry.WriteToFile(target, new ExtractionOptions { Overwrite = true });
        }
    }

    private static void ExtractTar(string path, string destination)
    {
        using var stream = File.OpenRead(path);
        using var reader = ReaderFactory.Open(stream);
        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.IsDirectory) continue;
            string target = PrepareTarget(destination, reader.Entry.Key);
            reader.WriteEntryToFile(target, new ExtractionOptions { Overwrite = true });
        }
    }

    private static string PrepareTarget(string destination, string key)
    {
        string target = Path.GetFullPath(Path.Combine(destination, key ?? ""));
        if (!ModInstallTarget.IsInside(destination, target))
            throw new InvalidDataException($"Archive entry '{key}' is outside the mod folder");
        string directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        return target;
    }

    private static int Match(string path)
    {
        string name = Path.GetFileName(path ?? "");
        for (int i = 0; i < Formats.Length; i++)
        {
            if (name.Length > Formats[i].Suffix.Length && name.EndsWith(Formats[i].Suffix, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }
}
