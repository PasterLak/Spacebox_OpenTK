
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Structures;

public class NotGeneratedEntity
{
    public long Id;
    public int radiusBlocks = 32;
    public AsteroidData asteroid;
    public Biome biome;
    public string FileName;
    public Vector3 positionInSector;
    public Vector3 positionWorld;
    public Vector3 rotation;

    public NotGeneratedEntity()
    {
    }

    public NotGeneratedEntity(long id, int radiusBlocks, AsteroidData asteroid, Biome biome, Vector3 positionInSector, Vector3 positionWorld, Vector3 rotation)
    {
        Id = id;
        this.radiusBlocks = radiusBlocks;
        this.asteroid = asteroid;
        this.biome = biome;
        this.positionInSector = positionInSector;
        this.positionWorld = positionWorld;
        this.rotation = rotation;
    }
}
