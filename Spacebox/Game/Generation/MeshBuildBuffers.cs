using Engine;

namespace Spacebox.Game.Generation;

public sealed class MeshBuildBuffers
{
    public const int InitialFaces = 4096;
    public const int MaxRetainedFaces = 65536;

    private const int FloatsPerFace = 4 * BuffersData.FloatsPerVertexBlock;
    private const int IndicesPerFace = 6;

    public static readonly ConcurrentPool<MeshBuildBuffers> Pool = new ConcurrentPool<MeshBuildBuffers>(
        () => new MeshBuildBuffers(),
        buffers => buffers.Trim(),
        Math.Max(4, Environment.ProcessorCount));

    public float[] Vertices;
    public uint[] Indices;

    public MeshBuildBuffers()
    {
        Allocate();
    }

    public void Trim()
    {
        if (Vertices.Length > MaxRetainedFaces * FloatsPerFace || Indices.Length > MaxRetainedFaces * IndicesPerFace)
            Allocate();
    }

    private void Allocate()
    {
        Vertices = new float[InitialFaces * FloatsPerFace];
        Indices = new uint[InitialFaces * IndicesPerFace];
    }
}
