using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Network.Containers;

namespace Spacebox.Network.Npc;

public sealed class SpacerNetwork : NetworkTransform, ISpacerLink
{
    private const int MaxDrillDamage = 20;
    private const float MaxPushSpeed = 10f;

    public bool IsReplica => !IsServer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Owner is not Spacer spacer) return;

        spacer.Link = this;
        if (IsReplica) spacer.MakeReplica();
    }

    public override void OnNetworkDespawn()
    {
        if (Owner is Spacer spacer && spacer.Link == this) spacer.Link = null;
    }

    public void SendDrillDamage(int damage)
    {
        SendRpc(nameof(DrillDamage), damage);
    }

    public void SendPush(Vector3 normal, float targetSpeed)
    {
        SendRpc(nameof(PushRequest), normal, targetSpeed);
    }

    public void BroadcastDamageEffect()
    {
        SendRpc(nameof(DamageEffect));
    }

    public void NotifyOpened()
    {
        if (Owner is Spacer spacer) ContainerNetwork.OnInteract(spacer, NetworkObject);
    }

    public void NotifyKilled()
    {
        if (IsServer && NetworkObject != null) NetworkManager.Instance.Spawner.Despawn(NetworkObject);
    }

    [Rpc(RpcTarget.Server, RequireOwnership = false)]
    private void DrillDamage(int damage)
    {
        if (Owner is Spacer spacer) spacer.ApplyDamage(Math.Clamp(damage, 0, MaxDrillDamage));
    }

    [Rpc(RpcTarget.Server, RequireOwnership = false, Delivery = DeliveryMode.Unreliable)]
    private void PushRequest(Vector3 normal, float targetSpeed)
    {
        if (Owner is not Spacer spacer || normal.LengthSquared < 0.0001f) return;
        spacer.Push(normal.Normalized(), Math.Clamp(targetSpeed, 0f, MaxPushSpeed));
    }

    [Rpc(RpcTarget.Clients)]
    private void DamageEffect()
    {
        if (Owner is Spacer spacer) spacer.PlayDamageEffect();
    }
}
