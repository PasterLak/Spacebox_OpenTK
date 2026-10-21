using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class SignBlock : InteractiveBlock, IEntityBlockListener, INetworkStateBlock
{
    private readonly SignBlockData _data;
    private readonly List<Vector3i> _extraCells = new List<Vector3i>();
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
        SignUI.Open(this);
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
        this.chunk = chunk;
        Entity = chunk.SpaceEntity;
        LocalCell = localCell;

        SignBlockData.GetExtraCells(_data.Width, Direction, Rotation, _extraCells);
        if (_extraCells.Count > 0)
        {
            for (int i = 0; i < _extraCells.Count; i++) _extraCells[i] += localCell;
            Entity.Reservations.Set(this, _extraCells);
        }

        Entity.Signs.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        Entity?.Reservations.Remove(this);
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
}
