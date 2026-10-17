using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct WeaponFireMessage : INetMessage
{
    public short ItemId;
    public Vector3 Origin;
    public Vector3 Direction;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(Origin);
        writer.Write(Direction);
    }

    public void Deserialize(NetReader reader)
    {
        ItemId = reader.ReadShort();
        Origin = reader.ReadVector3();
        Direction = reader.ReadVector3();
    }
}

public struct ProjectileSpawnMessage : INetMessage
{
    public int ShooterPeer;
    public short ItemId;
    public Vector3 Origin;
    public Vector3 Direction;
    public float Age;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ShooterPeer);
        writer.Write(ItemId);
        writer.Write(Origin);
        writer.Write(Direction);
        writer.Write(Age);
    }

    public void Deserialize(NetReader reader)
    {
        ShooterPeer = reader.ReadInt();
        ItemId = reader.ReadShort();
        Origin = reader.ReadVector3();
        Direction = reader.ReadVector3();
        Age = reader.ReadFloat();
    }
}

public struct PlayerDamageMessage : INetMessage
{
    public int AttackerPeer;
    public int Damage;
    public bool Headshot;

    public void Serialize(NetWriter writer)
    {
        writer.Write(AttackerPeer);
        writer.Write(Damage);
        writer.Write(Headshot);
    }

    public void Deserialize(NetReader reader)
    {
        AttackerPeer = reader.ReadInt();
        Damage = reader.ReadInt();
        Headshot = reader.ReadBool();
    }
}

public struct HitConfirmMessage : INetMessage
{
    public bool Headshot;

    public void Serialize(NetWriter writer) => writer.Write(Headshot);
    public void Deserialize(NetReader reader) => Headshot = reader.ReadBool();
}
