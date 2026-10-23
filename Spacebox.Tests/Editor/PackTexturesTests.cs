using Spacebox.Editor.Data;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.Editor;

public sealed class PackTexturesTests
{
    private sealed class FakePack : ITextureOverrideSource
    {
        public IReadOnlyDictionary<string, string> BlockTextures { get; init; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> ItemTextures { get; init; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Emissions { get; init; } = new Dictionary<string, string>();
    }

    [Fact]
    public void Rows_ShowWhatThePackReplacesAndWhatItDoesNotUse()
    {
        var index = new GameSetAssetIndex();
        index.BlockTextures["rock"] = "base/Blocks/rock.png";
        index.BlockTextures["ice"] = "base/Blocks/ice.jpg";
        index.ItemEmissions["lamp"] = "base/Items/Emissions/lamp.png";
        var pack = new FakePack
        {
            BlockTextures = new Dictionary<string, string> { ["rock"] = "pack/Blocks/rock.png", ["old"] = "pack/Blocks/old.png" },
            Emissions = new Dictionary<string, string> { ["lamp"] = "pack/Emissions/lamp.png" }
        };

        var rows = PackTextures.Build(index, pack);

        Assert.Contains(rows, r => r.Kind == PackTextureKind.Block && r.Name == "rock" && r.InPack && !r.Unused);
        Assert.Contains(rows, r => r.Kind == PackTextureKind.Block && r.Name == "ice" && !r.InPack);
        Assert.Contains(rows, r => r.Kind == PackTextureKind.Block && r.Name == "old" && r.Unused);
        Assert.Contains(rows, r => r.Kind == PackTextureKind.Glow && r.Name == "lamp" && r.InPack);
    }

    [Fact]
    public void Copies_GoIntoTheKindFolderWithTheBaseExtension()
    {
        var row = new PackTextureRow(PackTextureKind.Block, "ice", "base/Blocks/ice.jpg", null);
        var glow = new PackTextureRow(PackTextureKind.Glow, "lamp", "base/Items/Emissions/lamp.png", null);

        Assert.Equal(Path.Combine("pack", "Blocks", "ice.jpg"), PackTextures.TargetPath("pack", row));
        Assert.Equal(Path.Combine("pack", "Emissions", "lamp.png"), PackTextures.TargetPath("pack", glow));
    }
}
