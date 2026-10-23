using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Player.GameModes;
using Spacebox.Network.Mobs;

namespace Spacebox.Game.Player.Interactions;

public sealed class InteractionSpawnMob : InteractionMode
{
    private const float ReachDistance = 6f;
    private const float FreeSpawnDistance = 3f;
    private const float SurfaceOffset = 0.6f;

    private readonly Ray _ray = new Ray(Vector3.Zero, Vector3.UnitZ, ReachDistance);
    private ItemSlot? _slot;
    private float _cooldown;

    public InteractionSpawnMob(ItemSlot slot)
    {
        _slot = slot;
    }

    public override void OnEnable()
    {
        _cooldown = 0f;
    }

    public override void OnDisable()
    {
    }

    public override void Update(LocalAstronaut player)
    {
        if (_cooldown > 0f) _cooldown -= Time.Delta;
        if (!player.CanMove || _cooldown > 0f || !Input.IsActionDown("use")) return;
        if (_slot == null || !_slot.HasItem || _slot.Item is not MobSpawnerItem spawner) return;
        if (!Spacebox.Network.Containers.ContainerNetwork.CanUseItems) return;

        MobSpawnNetwork.RequestSpawn(spawner, FindSpawnPosition(player));
        _cooldown = spawner.UseCooldown;

        if (player.GameModeRules.ConsumeItems) _slot.DropOne();

        if (_slot.Count == 0)
        {
            _slot = null;
            player.SetInteraction(new InteractionDefault());
        }
    }

    private Vector3 FindSpawnPosition(LocalAstronaut player)
    {
        _ray.Origin = player.Position;
        _ray.Direction = player.Front;

        if (World.CurrentSector != null && World.CurrentSector.Raycast(_ray, out var hit))
            return hit.hitPosition + hit.normalWorld * SurfaceOffset;

        return player.Position + player.Front * FreeSpawnDistance;
    }
}
