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

        Assert.False(WorldInfoSaver.TryReadWorldInfo(path, out _));
    }

    [Fact]
    public void ValidWorldJson_IsRead()
    {
        string path = Path.Combine(_folder, "world.json");
        File.WriteAllText(path, "{\"Name\": \"Fine\"}");

        Assert.True(WorldInfoSaver.TryReadWorldInfo(path, out var info));
        Assert.Equal("Fine", info.Name);
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
