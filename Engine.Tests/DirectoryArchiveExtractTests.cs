using System.IO.Compression;
using Engine.Utils;

namespace Engine.Tests;

public sealed class DirectoryArchiveExtractTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "engine_archive_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private static byte[] Zip(params (string Name, int Size)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var (name, size) in entries)
            {
                using var entry = archive.CreateEntry(name).Open();
                entry.Write(new byte[size]);
            }
        }
        return stream.ToArray();
    }

    [Fact]
    public void Extract_WritesFiles()
    {
        DirectoryArchive.Extract(Zip(("a/b.txt", 10)), _root);

        Assert.Equal(10, new FileInfo(Path.Combine(_root, "a", "b.txt")).Length);
    }

    [Fact]
    public void Extract_RefusesTooLargeArchivesBeforeWritingAnything()
    {
        Assert.Throws<InvalidDataException>(() => DirectoryArchive.Extract(Zip(("a.bin", 600), ("b.bin", 600)), _root, maxExtractedBytes: 1000));
        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public void Extract_RefusesTooManyEntries()
    {
        Assert.Throws<InvalidDataException>(() => DirectoryArchive.Extract(Zip(("a", 1), ("b", 1), ("c", 1)), _root, maxEntries: 2));
    }

    [Fact]
    public void Extract_RefusesEntriesOutsideTheTarget()
    {
        Assert.Throws<InvalidDataException>(() => DirectoryArchive.Extract(Zip(("../evil.txt", 1)), Path.Combine(_root, "target")));
        Assert.False(File.Exists(Path.Combine(_root, "evil.txt")));
    }
}
