using System.IO.Compression;
using Engine.Utils;

namespace Engine.Tests.Utils;

public sealed class DirectoryArchiveTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("archive_tests").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    private string CreateGameSet(string name)
    {
        string folder = Path.Combine(_root, name);
        Directory.CreateDirectory(Path.Combine(folder, "Blocks"));
        File.WriteAllText(Path.Combine(folder, "config.json"), "{ \"ModId\": \"test\" }");
        File.WriteAllBytes(Path.Combine(folder, "Blocks", "stone.png"), new byte[] { 1, 2, 3, 4 });
        return folder;
    }

    [Fact]
    public void ComputeHash_IsStableForSameContent()
    {
        var a = CreateGameSet("a");
        var b = CreateGameSet("b");

        Assert.Equal(
            DirectoryArchive.ComputeHash([new ArchiveFolder(a, "mod")]),
            DirectoryArchive.ComputeHash([new ArchiveFolder(b, "mod")]));
    }

    [Fact]
    public void ComputeHash_ChangesWithContentNameOrEntry()
    {
        var folder = CreateGameSet("set");
        string original = DirectoryArchive.ComputeHash([new ArchiveFolder(folder, "mod")]);

        Assert.NotEqual(original, DirectoryArchive.ComputeHash([new ArchiveFolder(folder, "other")]));

        File.WriteAllText(Path.Combine(folder, "config.json"), "{ \"ModId\": \"changed\" }");
        string changedContent = DirectoryArchive.ComputeHash([new ArchiveFolder(folder, "mod")]);
        Assert.NotEqual(original, changedContent);

        File.Move(Path.Combine(folder, "Blocks", "stone.png"), Path.Combine(folder, "Blocks", "rock.png"));
        Assert.NotEqual(changedContent, DirectoryArchive.ComputeHash([new ArchiveFolder(folder, "mod")]));
    }

    [Fact]
    public void PackThenExtract_RestoresFilesUnderEntryNames()
    {
        var mod = CreateGameSet("mod");
        var basic = CreateGameSet("basic");
        string target = Path.Combine(_root, "extracted");

        byte[] data = DirectoryArchive.Pack([new ArchiveFolder(mod, "MyMod"), new ArchiveFolder(basic, "Default")]);
        DirectoryArchive.Extract(data, target);

        Assert.Equal("{ \"ModId\": \"test\" }", File.ReadAllText(Path.Combine(target, "MyMod", "config.json")));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(target, "Default", "Blocks", "stone.png")));
        Assert.Equal(
            DirectoryArchive.ComputeHash([new ArchiveFolder(mod, "mod")]),
            DirectoryArchive.ComputeHash([new ArchiveFolder(Path.Combine(target, "MyMod"), "mod")]));
    }

    [Fact]
    public void Extract_RejectsEntriesOutsideTarget()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            using var writer = new StreamWriter(archive.CreateEntry("../evil.txt").Open());
            writer.Write("bad");
        }

        string target = Path.Combine(_root, "safe");

        Assert.Throws<InvalidDataException>(() => DirectoryArchive.Extract(stream.ToArray(), target));
        Assert.False(File.Exists(Path.Combine(_root, "evil.txt")));
    }

    [Fact]
    public void MissingFolder_HashesAndPacksAsEmpty()
    {
        string missing = Path.Combine(_root, "missing");

        Assert.Equal(
            DirectoryArchive.ComputeHash([]),
            DirectoryArchive.ComputeHash([new ArchiveFolder(missing, "mod")]));
        Assert.NotEmpty(DirectoryArchive.Pack([new ArchiveFolder(missing, "mod")]));
    }
}
