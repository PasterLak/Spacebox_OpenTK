using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class ModArchiveUnpackerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox-unpack-" + Guid.NewGuid().ToString("N"));
    private readonly ModFolders _folders;

    public ModArchiveUnpackerTests()
    {
        _folders = new ModFolders(_root);
        Directory.CreateDirectory(_folders.GameSets);
        Directory.CreateDirectory(_folders.Extensions);
        Directory.CreateDirectory(_folders.TexturePacks);
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    [Fact]
    public void TexturePackArchive_IsInstalledUnderItsName()
    {
        string archive = Path.Combine(_folders.TexturePacks, "pack.tar.gz");
        Write(archive, ArchiveType.Tar, CompressionType.GZip, ("Cool/pack.json", "{\"Id\":\"cool\",\"Name\":\"Cool Pack\"}"), ("Cool/Blocks/stone.png", "png"));

        ModArchiveUnpacker.UnpackFolder(_folders.TexturePacks, new TexturePackArchiveRule(_folders.TexturePacks));

        Assert.True(File.Exists(Path.Combine(_folders.TexturePacks, "Cool Pack", "Blocks", "stone.png")));
        Assert.False(File.Exists(archive));
    }

    [Fact]
    public void TexturePackArchive_WithoutPackJson_UsesTheArchiveName()
    {
        string archive = Path.Combine(_folders.TexturePacks, "Retro.zip");
        Write(archive, ArchiveType.Zip, CompressionType.Deflate, ("Items/drill.png", "png"));

        ModArchiveUnpacker.UnpackFolder(_folders.TexturePacks, new TexturePackArchiveRule(_folders.TexturePacks));

        Assert.True(File.Exists(Path.Combine(_folders.TexturePacks, "Retro", "Items", "drill.png")));
    }

    [Fact]
    public void TexturePackArchive_WithSameId_UpdatesTheInstalledPack()
    {
        var rule = new TexturePackArchiveRule(_folders.TexturePacks);
        string first = Path.Combine(_folders.TexturePacks, "v1.zip");
        Write(first, ArchiveType.Zip, CompressionType.Deflate, ("pack.json", "{\"Id\":\"cool\",\"Name\":\"Cool\"}"), ("Blocks/a.png", "old"));
        ModArchiveUnpacker.UnpackFolder(_folders.TexturePacks, rule);

        string second = Path.Combine(_folders.TexturePacks, "v2.zip");
        Write(second, ArchiveType.Zip, CompressionType.Deflate, ("pack.json", "{\"Id\":\"cool\",\"Name\":\"Cool\"}"), ("Blocks/a.png", "new"));
        ModArchiveUnpacker.UnpackFolder(_folders.TexturePacks, rule);

        Assert.Equal("new", File.ReadAllText(Path.Combine(_folders.TexturePacks, "Cool", "Blocks", "a.png")));
        Assert.Single(Directory.GetDirectories(_folders.TexturePacks));
    }

    [Fact]
    public void ArchiveWithoutPackContent_IsKept()
    {
        string archive = Path.Combine(_folders.TexturePacks, "random.zip");
        Write(archive, ArchiveType.Zip, CompressionType.Deflate, ("readme.txt", "hi"));

        ModArchiveUnpacker.UnpackFolder(_folders.TexturePacks, new TexturePackArchiveRule(_folders.TexturePacks));

        Assert.True(File.Exists(archive));
        Assert.Empty(Directory.GetDirectories(_folders.TexturePacks));
    }

    [Fact]
    public void ExtensionArchive_GoesToTheExtensionsFolder()
    {
        string archive = Path.Combine(_folders.Extensions, "ext.tar.gz");
        Write(archive, ArchiveType.Tar, CompressionType.GZip, ("Ext/config.json", "{\"ModName\":\"My Ext\",\"ModId\":\"my_ext\",\"Extends\":\"default\"}"));

        ModArchiveUnpacker.UnpackFolder(_folders.Extensions, new GameSetArchiveRule(_folders, true));

        Assert.True(File.Exists(Path.Combine(_folders.Extensions, "My Ext", "config.json")));
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
