using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Physics;
using Spacebox.Game.Pictures;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class PictureFrameBlock : InteractiveBlock, IEntityBlockListener, INetworkStateBlock
{
    public const short NoPicture = -1;

    private readonly PictureFrameBlockData _data;
    private PictureItem _picture;

    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public PictureFrameBlockData Data => _data;
    public PictureItem Picture => _picture;
    public int Version { get; private set; }

    public PictureFrameBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as PictureFrameBlockData ?? new PictureFrameBlockData(blockData, null, string.Empty);
        OnDestroy += DropPicture;
    }

    protected override bool ShowsUseHint => false;

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);

        var origin = hit.chunk?.SpaceEntity?.GetStructureOrigin(hit.chunk, hit.blockPositionIndex) as PictureFrameBlock ?? this;
        if (!origin.HasMultiblockOffset) PictureFrameInteraction.Use(origin, player);
    }

    public bool CanSwap(PictureItem picture)
    {
        if (picture == null) return _picture != null;
        return _data.Accepts(picture) && picture != _picture;
    }

    public bool TrySwap(PictureItem picture, out PictureItem removed)
    {
        removed = null;
        if (!CanSwap(picture)) return false;

        removed = _picture;
        SetPicture(picture);
        if (chunk != null) chunk.IsModified = true;
        if (Entity != null) Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, LocalCell, this);
        return true;
    }

    public void RestorePicture(PictureItem picture)
    {
        SetPicture(picture != null && _data.Accepts(picture) ? picture : null);
    }

    public void WriteNetworkState(NetWriter writer)
    {
        writer.Write(_picture?.Id ?? NoPicture);
    }

    public void ReadNetworkState(NetReader reader)
    {
        short id = reader.ReadShort();
        SetPicture(id != NoPicture && GameAssets.Items.TryGetValue(id, out var item) ? item as PictureItem : null);
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        if (HasMultiblockOffset) return;

        this.chunk = chunk;
        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        Entity.Pictures.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        Entity?.Pictures.Unregister(this);
        Entity = null;
    }

    private void SetPicture(PictureItem picture)
    {
        if (picture == _picture) return;

        _picture = picture;
        Version++;
    }

    private void DropPicture(HitInfo info)
    {
        if (Spacebox.Network.GameSession.IsRemoteClient) return;

        var frame = this;
        if (HasMultiblockOffset && info.chunk?.SpaceEntity != null)
            frame = info.chunk.SpaceEntity.GetStructureOrigin(info.chunk, info.blockPositionIndex) as PictureFrameBlock;

        if (frame?._picture == null) return;
        World.DropEffectManager?.DropItem(info.blockPositionWorld, Vector3.Zero, 0f, frame._picture, 1);
        frame.SetPicture(null);
    }
}
