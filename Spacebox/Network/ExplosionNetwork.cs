using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics.Explosions;
using Spacebox.Game.Resource;
using Spacebox.Game.Rules;

namespace Spacebox.Network;

public struct ExplosionMessage : INetMessage
{
    public long EntityId;
    public Vector3i Cell;
    public short BombBlockId;
    public List<Vector3i> Cells;
    public byte Flags;
    public int CauserPeer;

    private const byte FlagExplode = 1;
    private const byte FlagDamagePlayers = 2;
    private const byte FlagPvp = 4;
    private const byte FlagRespectClaims = 8;

    public static byte Pack(ExplosionRules rules)
    {
        return (byte)((rules.Explode ? FlagExplode : 0) | (rules.DamagePlayers ? FlagDamagePlayers : 0) | (rules.Pvp ? FlagPvp : 0) | (rules.RespectClaims ? FlagRespectClaims : 0));
    }

    public ExplosionRules Rules => new ExplosionRules((Flags & FlagExplode) != 0, false, (Flags & FlagDamagePlayers) != 0, (Flags & FlagRespectClaims) != 0, (Flags & FlagPvp) != 0, CauserPeer);

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Cell);
        writer.Write(BombBlockId);
        writer.Write(Flags);
        writer.Write(CauserPeer);
        writer.Write(Cells.Count);
        for (int i = 0; i < Cells.Count; i++)
        {
            writer.Write((sbyte)(Cells[i].X - Cell.X));
            writer.Write((sbyte)(Cells[i].Y - Cell.Y));
            writer.Write((sbyte)(Cells[i].Z - Cell.Z));
        }
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Cell = reader.ReadVector3i();
        BombBlockId = reader.ReadShort();
        Flags = reader.ReadByte();
        CauserPeer = reader.ReadInt();
        int count = reader.ReadCount(3);
        Cells = new List<Vector3i>(count);
        for (int i = 0; i < count; i++)
            Cells.Add(Cell + new Vector3i(reader.ReadSByte(), reader.ReadSByte(), reader.ReadSByte()));
    }
}

public static class ExplosionNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ExplosionMessage>(OnExplosion);
        ExplosionSystem.Exploded += OnExploded;
    }

    private static void OnExploded(SpaceEntity entity, Vector3i cell, short bombBlockId, IReadOnlyList<Vector3i> cells, ExplosionRules rules)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || network.Peers.Count == 0) return;

        network.SendToClients(new ExplosionMessage
        {
            EntityId = entity.EntityID,
            Cell = cell,
            BombBlockId = bombBlockId,
            Cells = new List<Vector3i>(cells),
            Flags = ExplosionMessage.Pack(rules),
            CauserPeer = rules.CauserPeer
        });
    }

    private static void OnExplosion(int sender, ExplosionMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnExplosion(sender, message))) return;

        var world = World.Instance;
        if (world?.Explosions == null || !world.TryFindEntity(message.EntityId, out var entity)) return;
        if (!GameAssets.Blocks.TryGetValue(message.BombBlockId, out var blockData) || blockData is not IExplosiveBlockData { Explosion: { } data }) return;

        world.Explosions.ApplyReplica(entity, message.Cell, data, message.Cells, message.Rules);
    }
}
