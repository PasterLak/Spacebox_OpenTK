namespace Engine.Graphics.Rhi;

public enum BufferKind : byte
{
    Vertex,
    Index,
    Uniform
}

public enum BufferUsage : byte
{
    Static,
    Dynamic,
    Stream
}

public enum VertexComponentType : byte
{
    Float,
    Byte,
    UnsignedByte,
    Short,
    UnsignedShort,
    Int,
    UnsignedInt,
    HalfFloat,
    Double
}

public enum IndexType : byte
{
    UnsignedShort,
    UnsignedInt
}

public enum PrimitiveTopology : byte
{
    Points,
    Lines,
    LineStrip,
    LineLoop,
    Triangles,
    TriangleStrip,
    TriangleFan
}
