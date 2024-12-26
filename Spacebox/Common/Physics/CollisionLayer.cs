

namespace Spacebox.Common.Physics
{
    [Flags]
    public enum CollisionLayer
    {
        None = 0,
        Default = 1 << 0,      // 1
        Terrain = 1 << 1,      // 2
        Player = 1 << 2,       // 4
        Enemy = 1 << 3,        // 8
        Projectile = 1 << 4,   // 16
                               // 
        All = ~0                // All
    }
}
