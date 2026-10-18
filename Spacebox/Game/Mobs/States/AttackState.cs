using Engine.AI;
using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs.States;

public sealed class AttackState : State<MobContext>
{
    private const float ArriveFactor = 0.6f;
    private const float FacingThreshold = 0.6f;

    protected override void OnEnter()
    {
        Context.Mob.Action = MobAction.Hunting;
    }

    protected override void OnUpdate(float deltaTime)
    {
        var senses = Context.Senses;
        if (!senses.HasTarget) return;

        var mob = Context.Mob;
        var definition = Context.Definition;
        var targetPosition = senses.TargetPosition;

        mob.LookAt(targetPosition);
        Context.Locomotion.MoveTo(targetPosition, definition.Speed, definition.AttackRange * ArriveFactor);

        if (Context.AttackCooldown > 0f || !senses.CanSeeTarget || !Context.IsTargetInAttackRange()) return;

        var toTarget = targetPosition - mob.Position;
        if (toTarget.LengthSquared > 1e-4f && Vector3.Dot(mob.Forward, toTarget.Normalized()) < FacingThreshold) return;

        mob.Bite(senses.TargetPeer);
        Context.AttackCooldown = definition.AttackCooldown;
    }

    protected override void OnExit()
    {
        Context.Locomotion.Stop();
    }
}
