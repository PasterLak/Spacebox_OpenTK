using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Protection;

public static class ClaimPlacement
{
    public static bool IsBeacon(short blockId) => GameAssets.GetBlockDataById(blockId) is ClaimBeaconBlockData;

    public static bool CheckLocal(SpaceEntity entity, Vector3i cell, Block block)
    {
        if (block is not ClaimBeaconBlock || entity == null) return true;

        string error = ClaimRegistry.CheckPlacement(entity, cell, Spacebox.Network.GameSession.PlayerName);
        if (error == null) return true;

        GUI.Chat.Write(error, Color4.Red);
        return false;
    }

    public static string CheckNewEntity(Block block, Vector3 worldPosition, string owner)
    {
        return block is ClaimBeaconBlock ? ClaimRegistry.CheckNewEntityPlacement(worldPosition, owner) : null;
    }

    public static void OnPlacedLocally(Block block)
    {
        if (block is not ClaimBeaconBlock beacon) return;
        beacon.SetOwner(Spacebox.Network.GameSession.PlayerName);
        ClaimVisuals.Highlight(beacon);
    }

    public static void AssignOwner(SpaceEntity entity, Vector3i cell, string owner)
    {
        if (entity == null || !entity.TryGetBlockLocal(cell, out var chunk, out var index)) return;
        if (chunk.Blocks[index.X, index.Y, index.Z] is ClaimBeaconBlock beacon) beacon.SetOwner(owner);
    }
}
