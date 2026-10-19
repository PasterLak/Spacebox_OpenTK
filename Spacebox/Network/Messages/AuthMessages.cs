using Engine.Networking;

namespace Spacebox.Network.Messages;

public struct AuthChallengeMessage : INetMessage
{
    public bool IsRegistered;
    public byte[] Salt;
    public int Iterations;
    public byte[] Nonce;
    public string Error;

    public void Serialize(NetWriter writer)
    {
        writer.Write(IsRegistered);
        writer.WriteBytes(Salt ?? Array.Empty<byte>());
        writer.Write(Iterations);
        writer.WriteBytes(Nonce ?? Array.Empty<byte>());
        writer.Write(Error ?? "");
    }

    public void Deserialize(NetReader reader)
    {
        IsRegistered = reader.ReadBool();
        Salt = reader.ReadBytes();
        Iterations = reader.ReadInt();
        Nonce = reader.ReadBytes();
        Error = reader.ReadString();
    }
}

public struct AuthResponseMessage : INetMessage
{
    public byte[] Key;
    public byte[] Proof;

    public void Serialize(NetWriter writer)
    {
        writer.WriteBytes(Key ?? Array.Empty<byte>());
        writer.WriteBytes(Proof ?? Array.Empty<byte>());
    }

    public void Deserialize(NetReader reader)
    {
        Key = reader.ReadBytes();
        Proof = reader.ReadBytes();
    }
}
