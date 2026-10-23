using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Mobs;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Network.Players;

namespace Spacebox.Network.Mobs;

public sealed class MobNetwork : NetworkTransform, IMobLink
{
    private const int MaxDrillDamage = 20;
    private const float MaxPushSpeed = 10f;

    private readonly Combat.DrillDamageBudget _drillBudget = new Combat.DrillDamageBudget();

    private readonly NetworkVariable<byte> _action;

    public bool IsReplica => !IsServer;

    public MobNetwork()
    {
        PositionThreshold = 0.02f;
        _action = CreateVariable((byte)MobAction.Idle);
        _action.Changed += OnActionChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Owner is not Mob mob) return;

        mob.Link = this;
        if (!IsReplica) return;

        mob.MakeReplica();
        mob.ApplyReplicatedAction((MobAction)_action.Value, false);
    }

    public override void OnNetworkDespawn()
    {
        if (Owner is Mob mob && mob.Link == this) mob.Link = null;
    }

    public void SendDrillDamage(int damage)
    {
        SendRpc(nameof(DrillDamage), damage);
    }

    public void SendPush(Vector3 normal, float targetSpeed)
    {
        SendRpc(nameof(PushRequest), normal, targetSpeed);
    }

    public void SyncAction(MobAction action)
    {
        if (IsServer) _action.Value = (byte)action;
    }

    public void BroadcastHurt()
    {
        SendRpc(nameof(Hurt));
    }

    public void BroadcastBite(int targetPeer, int damage)
    {
        SendRpc(nameof(Bite), targetPeer, damage);
    }

    public void BroadcastEating(Vector3 surface, Vector3 direction, short blockId, float duration)
    {
        SendRpc(nameof(Eating), surface, direction, blockId, duration);
    }

    public void BroadcastDeath(short effectBlockId)
    {
        SendRpc(nameof(Death), effectBlockId);
    }

    public void Despawn()
    {
        if (IsServer && NetworkObject != null) NetworkManager.Instance.Spawner.Despawn(NetworkObject);
    }

    [Rpc(RpcTarget.Server, RequireOwnership = false)]
    private void DrillDamage(int damage)
    {
        if (Owner is not Mob mob) return;
        int allowed = _drillBudget.Consume(RpcContext.SenderPeer, mob.Position, Math.Clamp(damage, 0, MaxDrillDamage));
        if (allowed > 0) mob.ApplyDamage(allowed);
    }

    [Rpc(RpcTarget.Server, RequireOwnership = false, Delivery = DeliveryMode.Unreliable)]
    private void PushRequest(Vector3 normal, float targetSpeed)
    {
        if (Owner is not Mob mob || !NetValidation.IsFinite(normal) || !float.IsFinite(targetSpeed) || normal.LengthSquared < 0.0001f) return;
        if (!PushRange.IsNear(RpcContext.SenderPeer, mob.Position)) return;
        mob.Push(normal.Normalized(), Math.Clamp(targetSpeed, 0f, MaxPushSpeed));
    }

    [Rpc(RpcTarget.Clients)]
    private void Hurt()
    {
        if (Owner is Mob mob) mob.PlayHurt();
    }

    [Rpc(RpcTarget.Clients)]
    private void Bite(int targetPeer, int damage)
    {
        if (Owner is not Mob mob) return;

        mob.PlayBite();
        if (targetPeer == NetworkManager.Instance.LocalPeerId) DamageLocalPlayer(mob, damage);
    }

    [Rpc(RpcTarget.Clients)]
    private void Eating(Vector3 surface, Vector3 direction, short blockId, float duration)
    {
        if (Owner is Mob mob) mob.PlayEatingEffect(surface, direction, blockId, duration);
    }

    [Rpc(RpcTarget.Clients)]
    private void Death(short effectBlockId)
    {
        if (Owner is Mob mob) mob.PlayDeathEffect(effectBlockId);
    }

    private void OnActionChanged(byte previous, byte current)
    {
        if (IsReplica && Owner is Mob mob) mob.ApplyReplicatedAction((MobAction)current);
    }

    private static void DamageLocalPlayer(Mob mob, int damage)
    {
        if (AstronautNetwork.Local?.Owner is not LocalAstronaut local) return;
        if (!local.IsAlive || !local.GameModeRules.MobsAttack) return;

        local.TakeDamage(damage, new DeathCase($"Eaten by {mob.Definition.Name}"));
    }
}
