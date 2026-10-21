using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Mobs;

namespace Spacebox.Game.Player.Footsteps;

public enum FootstepSurface : byte
{
    Stone,
    Metal
}

public static class FootstepSurfaces
{
    public static FootstepSurface For(Block block)
    {
        return For(block != null && NaturalBlocks.Contains(block.Id));
    }

    public static FootstepSurface For(bool isNatural) => isNatural ? FootstepSurface.Stone : FootstepSurface.Metal;
}
