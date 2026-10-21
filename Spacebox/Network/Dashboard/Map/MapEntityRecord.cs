using OpenTK.Mathematics;

namespace Spacebox.Network.Dashboard.Map;

public enum MapPersistence : byte
{
    Regenerated,
    Modified,
    Saved
}

public readonly struct MapEntityRecord
{
    public readonly long Id;
    public readonly Vector3 Position;
    public readonly float Radius;
    public readonly bool IsShip;
    public readonly bool IsLoaded;
    public readonly MapPersistence Persistence;

    public MapEntityRecord(long id, Vector3 position, float radius, bool isShip, bool isLoaded, MapPersistence persistence)
    {
        Id = id;
        Position = position;
        Radius = radius;
        IsShip = isShip;
        IsLoaded = isLoaded;
        Persistence = persistence;
    }
}

public readonly struct MapPlayerRecord
{
    public readonly string Name;
    public readonly Vector3 Position;
    public readonly bool IsAlive;

    public MapPlayerRecord(string name, Vector3 position, bool isAlive)
    {
        Name = name;
        Position = position;
        IsAlive = isAlive;
    }
}

public readonly struct MapClaimRecord
{
    public readonly string Owner;
    public readonly bool IsActive;

    public MapClaimRecord(string owner, bool isActive)
    {
        Owner = owner;
        IsActive = isActive;
    }
}
