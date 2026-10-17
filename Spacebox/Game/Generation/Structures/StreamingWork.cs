namespace Spacebox.Game.Generation;

public struct StreamingWork
{
    public int Entities;
    public int Chunks;
    public int Meshes;

    public int Total => Entities + Chunks + Meshes;
}
