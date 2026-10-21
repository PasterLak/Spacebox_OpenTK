using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player.Interactions;

public static class BlockUseTarget
{
    private static readonly Ray _ray = new Ray(Vector3.Zero, Vector3.UnitZ, InteractiveBlock.InteractionDistance);

    public static bool Update(LocalAstronaut player)
    {
        if (!TryFind(player, out var block, out var hit)) return false;

        InteractiveBlock.UpdateInteractive(block, player, ref hit);
        if (hit.block.Is<StorageBlock>(out var storageBlock)) storageBlock.SetPositionInChunk(hit.blockPositionIndex);
        return true;
    }

    public static bool TryFind(LocalAstronaut player, out InteractiveBlock block, out HitInfo hit)
    {
        block = null;
        hit = default;
        if (World.CurrentSector == null) return false;

        _ray.Origin = player.Position;
        _ray.Direction = player.Front;
        if (!World.CurrentSector.Raycast(_ray, out hit) || hit.block == null) return false;
        return hit.block.Is(out block);
    }
}
