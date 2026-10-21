using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class SignBlock : InteractiveBlock, IEntityBlockListener, INetworkStateBlock
{
    private readonly SignBlockData _data;
    private string _text;

    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public SignBlockData Data => _data;
    public string Text => _text;
    public int Version { get; private set; }
    public bool IsDefaultText => _text == _data.DefaultText;

    public SignBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as SignBlockData ?? new SignBlockData(blockData, null);
        _text = _data.DefaultText;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);

        var origin = HasMultiblockOffset ? FindOrigin(ref hit) : this;
        if (origin != null) SignUI.Open(origin);
    }

    public bool SetText(string text)
    {
        string sanitized = _data.SanitizeText(text);
        if (sanitized == _text) return false;

        _text = sanitized;
        Version++;
        if (chunk != null) chunk.IsModified = true;
        Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, LocalCell, this);
        return true;
    }

    public void RestoreText(string text)
    {
        _text = _data.SanitizeText(text ?? "");
        Version++;
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        if (HasMultiblockOffset) return;

        this.chunk = chunk;
        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        Entity.Signs.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        Entity?.Signs.Unregister(this);
        Entity = null;
    }

    public void WriteNetworkState(Engine.Networking.NetWriter writer)
    {
        writer.Write(_text);
    }

    public void ReadNetworkState(Engine.Networking.NetReader reader)
    {
        SetText(reader.ReadString() ?? "");
    }

    private SignBlock FindOrigin(ref HitInfo hit)
    {
        if (hit.chunk == null) return null;

        var entity = hit.chunk.SpaceEntity;
        var cell = Spacebox.Game.Protection.ProtectionRules.CellOf(hit.chunk, hit.blockPositionIndex);
        var origin = cell - MultiblockLayout.GetOriginOffset(_data, this);
        if (!entity.TryGetBlockLocal(origin, out var originChunk, out var index)) return null;
        return originChunk.Blocks[index.X, index.Y, index.Z] as SignBlock;
    }
}
