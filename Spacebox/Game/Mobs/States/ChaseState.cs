using Engine.AI;

namespace Spacebox.Game.Mobs.States;

public sealed class ChaseState : State<MobContext>
{
    private const float ArriveFactor = 0.7f;

    protected override void OnEnter()
    {
        Context.Mob.Action = MobAction.Hunting;
    }

    protected override void OnUpdate(float deltaTime)
    {
        var senses = Context.Senses;
        if (!senses.HasTarget) return;

        Context.Locomotion.MoveTo(senses.TargetPosition, Context.Definition.ChaseSpeed, Context.Definition.AttackRange * ArriveFactor);
        if (senses.CanSeeTarget) Context.Mob.LookAt(senses.TargetPosition);
    }

    protected override void OnExit()
    {
        Context.Locomotion.Stop();
    }
}
