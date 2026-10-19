using OpenTK.Mathematics;

namespace Engine.Networking;

public readonly record struct TransformState(float Time, Vector3 Position, Quaternion Rotation)
{
    public static void Write(NetWriter writer, TransformState state)
    {
        writer.Write(state.Time);
        writer.Write(state.Position);
        writer.Write(state.Rotation);
    }

    public static TransformState Read(NetReader reader)
    {
        return new TransformState(reader.ReadFloat(), reader.ReadVector3(), reader.ReadQuaternion());
    }
}
