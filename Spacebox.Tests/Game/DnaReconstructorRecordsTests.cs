using Engine.Networking;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player.Respawn;
using Spacebox.Game.Versioning;
using Spacebox.Network.Players;
using Xunit;

namespace Spacebox.Tests;

public class DnaReconstructorRecordsTests : IDisposable
{
    private readonly string _world = Path.Combine(Path.GetTempPath(), "spacebox_reconstructors_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_world)) Directory.Delete(_world, true);
    }

    private static ReconstructorKey Key(long entity, int x = 0) => new ReconstructorKey(entity, new Vector3i(x, 0, 0));

    private static ReconstructorCandidate Candidate(long entity, float x, bool dna = true, bool power = true) => new ReconstructorCandidate(Key(entity), new Vector3(x, 0, 0), dna, power);

    [Fact]
    public void Selector_PicksTheNearestReadyOneAndSkipsExcluded()
    {
        var candidates = new List<ReconstructorCandidate> { Candidate(1, 50000f), Candidate(2, 10f, power: false), Candidate(3, 900f) };

        Assert.True(ReconstructorSelector.TryFindNearestReady(candidates, Vector3.Zero, null, out var nearest));
        Assert.Equal(3, nearest.Key.EntityId);

        Assert.True(ReconstructorSelector.TryFindNearestReady(candidates, Vector3.Zero, new HashSet<ReconstructorKey> { Key(3) }, out nearest));
        Assert.Equal(1, nearest.Key.EntityId);
    }

    [Fact]
    public void Selector_ExplainsWhyNoneIsReady()
    {
        Assert.Equal(ReconstructorSelector.NoReconstructor, ReconstructorSelector.GetBlocker(new List<ReconstructorCandidate>()));
        Assert.Equal(ReconstructorSelector.NoDna, ReconstructorSelector.GetBlocker(new List<ReconstructorCandidate> { Candidate(1, 0, dna: false) }));
        Assert.Equal(ReconstructorSelector.NoPower, ReconstructorSelector.GetBlocker(new List<ReconstructorCandidate> { Candidate(1, 0, power: false) }));
        Assert.Null(ReconstructorSelector.GetBlocker(new List<ReconstructorCandidate> { Candidate(1, 0, power: false), Candidate(2, 5) }));
    }

    [Fact]
    public void Record_SplitsFarPositionsIntoSectorAndLocal()
    {
        var position = new Vector3(-8192 * 3 + 100.5f, 8192 * 40 + 7.25f, 12f);

        var record = ReconstructorRecord.At(Key(1), position, 2, true);

        Assert.Equal(new Vector3i(-3, 40, 0), record.Sector);
        Assert.Equal(new Vector3(100.5f, 7.25f, 12f), record.Local);
        Assert.Equal(position, record.WorldPosition);
    }

    [Fact]
    public void Records_RemoveEntityKeepsTheCellsStillThere()
    {
        var records = new ReconstructorRecords();
        records.Upsert(ReconstructorRecord.At(Key(1, 0), Vector3.Zero, 1, true));
        records.Upsert(ReconstructorRecord.At(Key(1, 5), Vector3.Zero, 1, true));
        records.Upsert(ReconstructorRecord.At(Key(2, 0), Vector3.Zero, 1, true));

        int removed = records.RemoveEntity(1, key => key.Cell.X == 5);

        Assert.Equal(1, removed);
        Assert.True(records.TryGet(Key(1, 5), out _));
        Assert.False(records.TryGet(Key(1, 0), out _));
        Assert.True(records.TryGet(Key(2, 0), out _));
    }

    [Fact]
    public void Records_CandidatesSkipLiveKeys()
    {
        var records = new ReconstructorRecords();
        records.Upsert(ReconstructorRecord.At(Key(1), Vector3.One, 0, true));
        records.Upsert(ReconstructorRecord.At(Key(2), Vector3.One, 3, false));

        var candidates = new List<ReconstructorCandidate>();
        records.CollectCandidates(candidates, new HashSet<ReconstructorKey> { Key(1) });

        var candidate = Assert.Single(candidates);
        Assert.Equal(Key(2), candidate.Key);
        Assert.True(candidate.HasDna);
        Assert.False(candidate.IsPowered);
    }

    [Fact]
    public void File_RoundTripsAndIsRemovedWhenEmpty()
    {
        Directory.CreateDirectory(_world);
        var record = ReconstructorRecord.At(new ReconstructorKey(-42, new Vector3i(-1, 33, 7)), new Vector3(8192 * 2 + 5, -4, 3), 4, true);

        ReconstructorRecordsFile.Write(_world, new[] { record });
        Assert.Equal(record, Assert.Single(ReconstructorRecordsFile.Read(_world)));

        ReconstructorRecordsFile.Write(_world, Array.Empty<ReconstructorRecord>());
        Assert.False(File.Exists(ReconstructorRecordsFile.PathOf(_world)));
        Assert.Empty(ReconstructorRecordsFile.Read(_world));
    }

    [Fact]
    public void Seeder_FindsReconstructorsInRotatedEntities()
    {
        var entity = new CompoundTag("SpaceEntity");
        entity.Add(new LongTag(NBTKey.ENTITY.id, -5));
        EntityTagLocation.Write(entity, new Vector3i(1, 0, 0), new Vector3(100, 0, 0));
        var rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        entity.Add(new FloatTag(NBTKey.ENTITY.rotation_x, rotation.X));
        entity.Add(new FloatTag(NBTKey.ENTITY.rotation_y, rotation.Y));
        entity.Add(new FloatTag(NBTKey.ENTITY.rotation_z, rotation.Z));
        entity.Add(new FloatTag(NBTKey.ENTITY.rotation_w, rotation.W));

        var chunk = new CompoundTag(null);
        chunk.Add(new ByteTag(NBTKey.CHUNK.index_x, PackingTools.SByteToByte(-1)));
        chunk.Add(new ByteTag(NBTKey.CHUNK.index_y, 0));
        chunk.Add(new ByteTag(NBTKey.CHUNK.index_z, 0));
        var blocks = new byte[32 * 32 * 32];
        int flat = (31 * 32 + 2) * 32 + 3;
        blocks[flat] = 1;
        chunk.Add(new ByteArrayTag(NBTKey.CHUNK.blocks, blocks));
        var palette = new ListTag(NBTKey.CHUNK.palette_blocks, TagType.String) { new StringTag(null, "default:air"), new StringTag(null, "default:dna_reconstructor") };
        chunk.Add(palette);
        var storages = new ListTag(NBTKey.CHUNK.storages, TagType.Compound);
        var storage = new CompoundTag(null);
        storage.Add(new IntTag("flat_index", flat));
        storage.Add(new IntTag("slot_data", PackingTools.PackShorts(0, 6)));
        storages.Add(storage);
        chunk.Add(storages);
        entity.Add(new ListTag(NBTKey.ENTITY.chunks, TagType.Compound) { chunk });

        var output = new List<ReconstructorRecord>();
        ReconstructorSeeder.Collect(entity, EntityStorageLayout.ReconstructorIds, output);

        var record = Assert.Single(output);
        Assert.Equal(new ReconstructorKey(-5, new Vector3i(-1, 2, 3)), record.Key);
        Assert.Equal(6, record.DnaCount);
        var expected = new Vector3(8192 + 100, 0, 0) + Vector3.Transform(new Vector3(-0.5f, 2.5f, 3.5f), rotation);
        Assert.True((record.WorldPosition - expected).Length < 1e-3f);
    }

    [Fact]
    public void ResultMessage_RoundTripsAGrant()
    {
        var grant = new ReconstructionGrant(new ReconstructorKey(-9, new Vector3i(1, -2, 3)), new Vector3(10, 20, 30), Quaternion.FromAxisAngle(Vector3.UnitX, 0.5f), 77, new Vector3(11, 21, 31));
        var writer = new NetWriter();
        ReconstructionResultMessage.From(4, grant).Serialize(writer);

        var copy = new ReconstructionResultMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));

        Assert.Equal(4, copy.RequestId);
        Assert.Equal(grant, copy.ToGrant());
    }

    [Fact]
    public void ResultMessage_RoundTripsARefusal()
    {
        var writer = new NetWriter();
        ReconstructionResultMessage.From(8, null).Serialize(writer);

        var copy = new ReconstructionResultMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));

        Assert.Equal(8, copy.RequestId);
        Assert.Null(copy.ToGrant());
    }

    [Fact]
    public void Anchors_KeepAPlaceObservedUntilTheyExpire()
    {
        WorldObservers.Clear();
        try
        {
            WorldObservers.SetRemote(new[] { Vector3.Zero });
            WorldObservers.AddAnchor(new Vector3(5000, 0, 0), 2f);

            Assert.Equal(2, WorldObservers.Remote.Count);
            Assert.True(WorldObservers.IsNearRemote(new Vector3(5010, 0, 0), 20f));

            WorldObservers.SetRemote(Array.Empty<Vector3>());
            WorldObservers.Update(1f);
            Assert.True(WorldObservers.IsNearRemote(new Vector3(5010, 0, 0), 20f));

            WorldObservers.Update(1.5f);
            Assert.Empty(WorldObservers.Remote);
        }
        finally
        {
            WorldObservers.Clear();
        }
    }
}
