using OpenTK.Mathematics;

namespace Spacebox.Game;

public interface ISpacerLink
{
    bool IsReplica { get; }
    void SendDrillDamage(int damage);
    void SendPush(Vector3 normal, float targetSpeed);
    void BroadcastDamageEffect();
    void NotifyOpened();
    void NotifyKilled();
}
