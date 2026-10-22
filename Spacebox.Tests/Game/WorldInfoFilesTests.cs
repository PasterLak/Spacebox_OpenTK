using Spacebox.Game.GUI;
using Xunit;

namespace Spacebox.Tests.Game;

public class WorldInfoFilesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox_worldinfo_" + Guid.NewGuid().ToString("N"));

    public WorldInfoFilesTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    [Fact]
    public void TruncatedWorldJson_IsSkipped()
    {
        string path = Path.Combine(_folder, "world.json");
        File.WriteAllText(path, "{\"Name\": \"Broken");

        Assert.False(WorldInfoFiles.TryRead(_folder, out _));
    }

    [Fact]
    public void ValidWorldJson_IsRead()
    {
        string path = Path.Combine(_folder, "world.json");
        File.WriteAllText(path, "{\"Name\": \"Fine\"}");

        Assert.True(WorldInfoFiles.TryRead(_folder, out var info));
        Assert.Equal("Fine", info.Name);
    }

    [Fact]
    public void Saving_WritesWorldNbtAndRemovesTheOldJson()
    {
        File.WriteAllText(Path.Combine(_folder, "world.json"), "{\"Name\": \"Old\", \"Seed\": \"42\", \"GameVersion\": \"0.1.5\"}");
        Assert.True(WorldInfoFiles.TryRead(_folder, out var info));

        info.Name = "New";
        WorldInfoFiles.Write(_folder, info);

        Assert.False(File.Exists(Path.Combine(_folder, "world.json")));
        Assert.True(File.Exists(Path.Combine(_folder, "world.nbt")));
        Assert.True(WorldInfoFiles.TryRead(_folder, out var loaded));
        Assert.Equal("New", loaded.Name);
        Assert.Equal("42", loaded.Seed);
        Assert.Equal("0.1.5", loaded.GameVersion);
    }

    [Fact]
    public void DamagedWorldNbt_IsSkipped()
    {
        File.WriteAllBytes(Path.Combine(_folder, "world.nbt"), new byte[] { 1, 2, 3 });
        Assert.False(WorldInfoFiles.TryRead(_folder, out _));
    }

    [Fact]
    public void EmptyPreview_DoesNotThrow()
    {
        string path = Path.Combine(_folder, "preview.jpg");
        File.WriteAllBytes(path, Array.Empty<byte>());
        var info = new WorldInfo();

        WorldInfo.LoadWorldPreview(path, info);
        WorldInfo.CreatePreviewFromPixels(info);

        Assert.Null(info.WorldPreview);
    }
}
