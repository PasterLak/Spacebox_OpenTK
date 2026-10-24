using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.GameSets;

public sealed class ModArchiveTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox-archives-" + Guid.NewGuid().ToString("N"));

    public ModArchiveTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    [Theory]
    [InlineData("mod.zip", "mod")]
    [InlineData("mod.rar", "mod")]
    [InlineData("mod.7z", "mod")]
    [InlineData("mod.tar", "mod")]
    [InlineData("mod.tar.gz", "mod")]
    [InlineData("mod.TGZ", "mod")]
    [InlineData("my.mod.tar.bz2", "my.mod")]
    [InlineData("mod.tbz2", "mod")]
    [InlineData("mod.tar.xz", "mod")]
    [InlineData("mod.txz", "mod")]
    [InlineData("mod.tar.zst", "mod")]
    public void KnownArchives_AreRecognisedAndNamed(string file, string name)
    {
        Assert.True(ModArchive.IsArchive(file));
        Assert.Equal(name, ModArchive.NameWithoutExtension(file));
    }

    [Theory]
    [InlineData("config.json")]
    [InlineData("mod.gz")]
    [InlineData(".zip")]
    [InlineData("mod")]
    public void OtherFiles_AreNotArchives(string file)
    {
        Assert.False(ModArchive.IsArchive(file));
    }

    [Theory]
    [InlineData("mod.zip", ArchiveType.Zip, CompressionType.Deflate)]
    [InlineData("mod.tar", ArchiveType.Tar, CompressionType.None)]
    [InlineData("mod.tar.gz", ArchiveType.Tar, CompressionType.GZip)]
    [InlineData("mod.tar.bz2", ArchiveType.Tar, CompressionType.BZip2)]
    public void Extract_WrittenArchives(string file, ArchiveType type, CompressionType compression)
    {
        string archive = Path.Combine(_root, file);
        Write(archive, type, compression, ("config.json", "{}"), ("Sub/a.txt", "hello"));

        string target = Path.Combine(_root, "out");
        ModArchive.Extract(archive, target);

        AssertModFiles(target, "{}");
    }

    [Theory]
    [InlineData("mod.7z")]
    [InlineData("mod.tar.xz")]
    public void Extract_FixtureArchives(string file)
    {
        string archive = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Archives", file);
        string target = Path.Combine(_root, "out");

        ModArchive.Extract(archive, target);

        AssertModFiles(target, "{\"ModName\":\"Archive Test\"}");
    }

    [Fact]
    public void Extract_RejectsEntriesOutsideTheFolder()
    {
        string archive = Path.Combine(_root, "evil.tar.gz");
        Write(archive, ArchiveType.Tar, CompressionType.GZip, ("../evil.txt", "bad"));
        string target = Path.Combine(_root, "out");

        Assert.Throws<InvalidDataException>(() => ModArchive.Extract(archive, target));
        Assert.False(File.Exists(Path.Combine(_root, "evil.txt")));
    }

    private static void AssertModFiles(string target, string config)
    {
        Assert.Equal(config, File.ReadAllText(Path.Combine(target, "config.json")));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(target, "Sub", "a.txt")));
    }

    private static void Write(string path, ArchiveType type, CompressionType compression, params (string Name, string Text)[] files)
    {
        using var stream = File.Create(path);
        using var writer = WriterFactory.Open(stream, type, new WriterOptions(compression));
        foreach (var (name, text) in files)
        {
            using var content = new MemoryStream(Encoding.UTF8.GetBytes(text));
            writer.Write(name, content, DateTime.Now);
        }
    }
}
