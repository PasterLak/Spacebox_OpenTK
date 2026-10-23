using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.ShipViewer;
using Spacebox.Game.Ships.Sharing;
using Spacebox.Game.Versioning;

namespace Spacebox.Tests.Ships;

public sealed class ShipSharingTests
{
    private static CompoundTag Entity(params (sbyte X, sbyte Y, sbyte Z)[] chunkIndices)
    {
        var chunks = new ListTag(NBTKey.ENTITY.chunks, TagType.Compound);
        foreach (var (x, y, z) in chunkIndices)
        {
            var chunk = new CompoundTag(null)
            {
                new ByteTag(NBTKey.CHUNK.index_x, PackingTools.SByteToByte(x)),
                new ByteTag(NBTKey.CHUNK.index_y, PackingTools.SByteToByte(y)),
                new ByteTag(NBTKey.CHUNK.index_z, PackingTools.SByteToByte(z)),
                new ListTag(NBTKey.CHUNK.palette_blocks, TagType.String) { new StringTag(null, "default:void"), new StringTag(null, "default:iron_block"), new StringTag(null, "mod:laser") }
            };
            var cells = new int[32 * 32 * 32];
            cells[0] = 1;
            cells[1] = 2;
            ChunkBlockIndices.Write(chunk, cells, 3);
            chunks.Add(chunk);
        }

        return new CompoundTag("entity")
        {
            new LongTag(NBTKey.ENTITY.id, -55),
            new StringTag(NBTKey.ENTITY.name, "Old"),
            chunks
        };
    }

    [Fact]
    public void File_RoundTripsMetadataAndOnlyKeepsChunks()
    {
        var ship = new ShipFile
        {
            Id = "my_ship", Name = "My Ship", Author = "Anna", Description = "Fast", GameVersion = "0.1.5",
            GameSets = { "default" }, Blocks = 2, Min = new Vector3i(0, 0, 0), Max = new Vector3i(1, 1, 2),
            Picture = new byte[] { 1, 2, 3 }, Entity = Entity((0, 0, 0))
        };

        var copy = ShipFileCodec.FromTag(ShipFileCodec.FromBytes(ShipFileCodec.ToBytes(ShipFileCodec.ToTag(ship))));

        Assert.Equal("My Ship", copy.Name);
        Assert.Equal("Anna", copy.Author);
        Assert.Equal(new[] { "default" }, copy.GameSets);
        Assert.Equal(new byte[] { 1, 2, 3 }, copy.Picture);
        Assert.Equal(new Vector3i(1, 1, 2), copy.Size);
        Assert.False(copy.Entity.ContainsKey(NBTKey.ENTITY.id));
        Assert.Single(ChunkTagAccess.Chunks(copy.Entity));
    }

    [Fact]
    public void Import_GetsNewIdPoseAndUnknownBlocksBecomeAir()
    {
        var missing = new SortedDictionary<string, int>();
        var rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathF.PI / 2f);

        var tag = ShipImportTag.Build(Entity((0, 0, 0), (1, 0, 0)), -999, new Vector3i(1, 2, 3), new Vector3(4, 5, 6), rotation, id => id == "default:iron_block", "default:void", missing);

        Assert.Equal(-999, tag.Get<LongTag>(NBTKey.ENTITY.id).Value);
        Assert.Equal("-999", tag.Get<StringTag>(NBTKey.ENTITY.name).Value);
        Assert.True(EntityTagLocation.TryReadSector(tag, out var sector));
        Assert.Equal(new Vector3i(1, 2, 3), sector);
        Assert.Equal(rotation.Y, tag.Get<FloatTag>(NBTKey.ENTITY.rotation_y).Value, 5);
        Assert.Equal(2, missing["mod:laser"]);
        Assert.Equal(1, ShipTagReader.Measure(tag).Blocks / 2);
        Assert.DoesNotContain("mod:laser", ChunkTagAccess.BlockPalette(ChunkTagAccess.Chunks(tag).First()));
    }

    [Fact]
    public void Validate_RejectsEmptyAndOutOfRangeShips()
    {
        Assert.NotNull(ShipImportTag.Validate(new CompoundTag("entity")));
        Assert.NotNull(ShipImportTag.Validate(Entity((8, 0, 0))));
        Assert.Null(ShipImportTag.Validate(Entity((-8, 7, 0))));
    }

    [Theory]
    [InlineData("My Super Ship!", "my_super_ship")]
    [InlineData("  ", "ship")]
    [InlineData("Кораблик 2", "2")]
    public void Ids_AreFileSafe(string name, string id)
    {
        Assert.Equal(id, ShipFileNames.MakeId(name));
    }

    [Fact]
    public void UniquePath_AddsANumber()
    {
        var taken = new HashSet<string> { Path.Combine("S", "a.ship"), Path.Combine("S", "a_2.ship") };
        Assert.Equal(Path.Combine("S", "a_3.ship"), ShipFileNames.UniquePath("S", "a", taken.Contains));
    }
}
