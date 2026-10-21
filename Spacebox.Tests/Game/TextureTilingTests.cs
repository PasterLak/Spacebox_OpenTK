using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game;

public class TextureTilingTests
{
    private const int OldBlockSize = 32;

    private static readonly (string Key, Direction Direction)[] FaceKeys =
    {
        ("Up", Direction.Up), ("Down", Direction.Down), ("Left", Direction.Left),
        ("Right", Direction.Right), ("Forward", Direction.Forward), ("Back", Direction.Back)
    };

    [Fact]
    public void FaceSizedTexturesSpan_SmallerTexturesRepeat()
    {
        var faces = new List<FaceTexture>
        {
            new FaceTexture(64, 64, 2, 2),
            new FaceTexture(32, 32, 2, 2)
        };

        int pixels = TextureTiling.ResolveCellPixels(faces, 0);

        Assert.Equal(32, pixels);
        Assert.Equal(new Vector2i(2, 2), TextureTiling.GetTiles(64, 64, pixels));
        Assert.Equal(new Vector2i(1, 1), TextureTiling.GetTiles(32, 32, pixels));
    }

    [Fact]
    public void HighResolutionBlock_WorksWithoutGlobalSize()
    {
        var faces = new List<FaceTexture>
        {
            new FaceTexture(128, 128, 2, 2),
            new FaceTexture(64, 64, 2, 2)
        };

        int pixels = TextureTiling.ResolveCellPixels(faces, 0);

        Assert.Equal(64, pixels);
        Assert.Equal(new Vector2i(2, 2), TextureTiling.GetTiles(128, 128, pixels));
        Assert.Equal(new Vector2i(1, 1), TextureTiling.GetTiles(64, 64, pixels));
    }

    [Fact]
    public void WallTextureOnNarrowSide_DoesNotChangeTheDensity()
    {
        var faces = new List<FaceTexture>
        {
            new FaceTexture(64, 96, 2, 3),
            new FaceTexture(64, 96, 1, 3),
            new FaceTexture(32, 32, 2, 1)
        };

        Assert.Equal(32, TextureTiling.ResolveCellPixels(faces, 0));
    }

    [Fact]
    public void TileSizedTexturesEverywhere_Repeat()
    {
        var faces = new List<FaceTexture>
        {
            new FaceTexture(32, 32, 2, 2),
            new FaceTexture(32, 32, 1, 2),
            new FaceTexture(32, 32, 2, 1)
        };

        int pixels = TextureTiling.ResolveCellPixels(faces, 0);

        Assert.Equal(32, pixels);
        Assert.Equal(new Vector2i(1, 1), TextureTiling.GetTiles(32, 32, pixels));
    }

    [Fact]
    public void SameSizedTexturesOnAllBigSides_FitTheirSides()
    {
        var faces = new List<FaceTexture>
        {
            new FaceTexture(128, 128, 2, 2),
            new FaceTexture(128, 128, 2, 2)
        };

        int pixels = TextureTiling.ResolveCellPixels(faces, 0);

        Assert.Equal(64, pixels);
        Assert.Equal(new Vector2i(2, 2), TextureTiling.GetTiles(128, 128, pixels));
    }

    [Fact]
    public void CellPixelsOverride_WinsOverDetection()
    {
        var faces = new List<FaceTexture> { new FaceTexture(64, 64, 2, 2) };

        Assert.Equal(64, TextureTiling.ResolveCellPixels(faces, 64));
        Assert.Equal(new Vector2i(1, 1), TextureTiling.GetTiles(64, 64, 64));
    }

    [Fact]
    public void NoTextures_GiveOnePixelAndOneTile()
    {
        Assert.Equal(1, TextureTiling.ResolveCellPixels(new List<FaceTexture>(), 0));
        Assert.Equal(new Vector2i(1, 1), TextureTiling.GetTiles(0, 0, 1));
    }

    [Fact]
    public void DefaultMultiCellBlocks_LookTheSameAsWithTheOldBlockSize()
    {
        string gameSet = GameSetFolder();
        var textureSizes = ReadTextureSizes(Path.Combine(gameSet, "Blocks"));
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(gameSet, "blocks.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        int checkedBlocks = 0;
        foreach (var block in document.RootElement.EnumerateArray())
        {
            if (!block.TryGetProperty("Size", out var sizeJson)) continue;
            var size = new Vector3Byte(Axis(sizeJson, "x"), Axis(sizeJson, "y"), Axis(sizeJson, "z"));
            if (size.X == 1 && size.Y == 1 && size.Z == 1) continue;
            if (block.TryGetProperty("TextureFit", out var fit) && fit.ValueKind == JsonValueKind.True) continue;

            var faceTextures = ReadFaceTextures(block, textureSizes);
            var faces = new List<FaceTexture>();
            foreach (var (direction, texture) in faceTextures)
            {
                MultiblockLayout.GetFaceCells(size, direction, out int columns, out int rows);
                faces.Add(new FaceTexture(texture.X, texture.Y, columns, rows));
            }

            int pixels = TextureTiling.ResolveCellPixels(faces, 0);
            foreach (var (direction, texture) in faceTextures)
            {
                MultiblockLayout.GetFaceCells(size, direction, out int columns, out int rows);
                var expected = Clamp(TextureTiling.GetTiles(texture.X, texture.Y, OldBlockSize), columns, rows);
                var actual = Clamp(TextureTiling.GetTiles(texture.X, texture.Y, pixels), columns, rows);
                Assert.True(expected == actual, $"{block.GetProperty("ID").GetString()} {direction}: expected {expected}, got {actual}");
            }
            checkedBlocks++;
        }

        Assert.True(checkedBlocks >= 10);
    }

    private static Vector2i Clamp(Vector2i tiles, int columns, int rows)
    {
        return new Vector2i(Math.Clamp(tiles.X, 1, columns), Math.Clamp(tiles.Y, 1, rows));
    }

    private static byte Axis(JsonElement size, string name)
    {
        foreach (var property in size.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value.GetByte();
        }
        return 1;
    }

    private static List<(Direction, Vector2i)> ReadFaceTextures(JsonElement block, Dictionary<string, Vector2i> sizes)
    {
        var result = new List<(Direction, Vector2i)>();
        if (!block.TryGetProperty("Textures", out var textures)) return result;

        foreach (var state in textures.EnumerateObject())
        {
            string all = state.Value.TryGetProperty("All", out var allJson) ? allJson.GetString() : null;
            foreach (var (key, direction) in FaceKeys)
            {
                string name = state.Value.TryGetProperty(key, out var faceJson) ? faceJson.GetString() : all;
                if (!string.IsNullOrEmpty(name) && sizes.TryGetValue(name.ToLowerInvariant(), out var textureSize))
                    result.Add((direction, textureSize));
            }
        }
        return result;
    }

    private static Dictionary<string, Vector2i> ReadTextureSizes(string folder)
    {
        var sizes = new Dictionary<string, Vector2i>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.png", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(file);
            var header = new byte[24];
            stream.ReadExactly(header);
            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            sizes.TryAdd(Path.GetFileNameWithoutExtension(file).ToLowerInvariant(), new Vector2i(width, height));
        }
        return sizes;
    }

    [Fact]
    public void FitStretchesTheTextureOverTheWholeFace()
    {
        var face = new FaceTexture(32, 32, 2, 3);

        Assert.Equal(new Vector2i(1, 1), TextureTiling.GetFaceTiles(face, 32, false));
        Assert.Equal(new Vector2i(2, 3), TextureTiling.GetFaceTiles(face, 32, true));
    }

    private static string GameSetFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Spacebox", "GameSets", "Default"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "Spacebox", "GameSets", "Default");
    }
}
