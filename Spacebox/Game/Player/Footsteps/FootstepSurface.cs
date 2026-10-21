using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Mobs;

namespace Spacebox.Game.Player.Footsteps;

public static class FootstepSurfaces
{
    public static readonly string[] Stone = { "footstepStone1", "footstepStone2", "footstepStone3", "footstepStone4" };
    public static readonly string[] Metal = { "footstepMetal1", "footstepMetal2", "footstepMetal3", "footstepMetal4" };

    public static string[] SoundsFor(Block block)
    {
        if (block == null) return Metal;

        var custom = GameAssets.GetBlockDataById(block.Id)?.FootstepSounds;
        return Resolve(custom, NaturalBlocks.Contains(block.Id));
    }

    public static string[] Resolve(string[] custom, bool isNatural)
    {
        if (custom != null && custom.Length > 0) return custom;
        return isNatural ? Stone : Metal;
    }
}
