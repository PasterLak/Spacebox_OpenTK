using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs;

public interface IMobLink
{
    bool IsReplica { get; }
    void SendDrillDamage(int damage);
    void SendPush(Vector3 normal, float targetSpeed);
    void SyncAction(MobAction action);
    void BroadcastHurt();
    void BroadcastBite(int targetPeer, int damage);
    void BroadcastEating(Vector3 surface, Vector3 direction, short blockId, float duration);
    void BroadcastDeath(short effectBlockId);
    void Despawn();
}
