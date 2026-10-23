using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player.Building;

namespace Spacebox.Network.Messages;

public struct PasteBlocksMessage : INetMessage
{
    public long EntityId;
    public Vector3i Min;
    public Vector3i Size;
    public List<ClipboardCell> Cells;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Min);
        writer.Write(Size);
        int count = Cells?.Count ?? 0;
        writer.Write((ushort)count);
        for (int i = 0; i < count; i++)
        {
            var cell = Cells[i];
            writer.Write((byte)cell.Offset.X);
            writer.Write((byte)cell.Offset.Y);
            writer.Write((byte)cell.Offset.Z);
            writer.Write(cell.BlockId);
            writer.Write((byte)cell.Direction);
            writer.Write((byte)cell.Rotation);
        }
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Min = reader.ReadVector3i();
        Size = reader.ReadVector3i();
        int count = reader.ReadUShort();
        Cells = new List<ClipboardCell>(Math.Min(count, BlockClipboard.MaxCells));
        for (int i = 0; i < count; i++)
        {
            var offset = new Vector3i(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            short id = reader.ReadShort();
            var direction = (Direction)reader.ReadByte();
            var rotation = (Rotation)reader.ReadByte();
            Cells.Add(new ClipboardCell(offset, id, direction, rotation));
        }
    }
}
