using Engine.AI;

namespace Spacebox.Game.Mobs.States;

public sealed class WatchState : State<MobContext>
{
    protected override void OnEnter()
    {
        Context.Mob.Action = MobAction.Idle;
        Context.Locomotion.Stop();
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (Context.Senses.HasTarget) Context.Mob.LookAt(Context.Senses.TargetPosition);
    }
}
