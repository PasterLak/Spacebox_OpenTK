using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public struct ShipPlaceRequestMessage : INetMessage
{
    public byte[] Data;
    public Vector3 Center;
    public Quaternion Rotation;

    public void Serialize(NetWriter writer)
    {
        writer.WriteBytes(Data ?? Array.Empty<byte>());
        writer.Write(Center);
        writer.Write(Rotation);
    }

    public void Deserialize(NetReader reader)
    {
        Data = reader.ReadBytes();
        Center = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
    }
}
