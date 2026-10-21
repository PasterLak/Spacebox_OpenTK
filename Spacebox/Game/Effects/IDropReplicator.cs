using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Effects;

public interface IDropReplicator
{
    bool IsAuthority { get; }
    void OnSpawned(Drop drop, Vector3 velocity, bool fromBlock, Color3Byte color);
    void OnQuantityChanged(Drop drop);
    void OnRemoved(Drop drop);
    void RequestSpawn(Item item, int quantity, Vector3 position, Vector3 velocity, float pickupDelay, float deceleration, SpaceEntity sourceEntity);
    void RequestPickup(Drop drop);
    void RequestDeathDrop(Vector3 position, IReadOnlyList<ItemStack> stacks);
}
