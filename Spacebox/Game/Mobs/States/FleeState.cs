using Engine.AI;

namespace Spacebox.Game.Mobs.States;

public sealed class FleeState : State<MobContext>
{
    private const float FleeDistance = 10f;
    private const float Jitter = 0.5f;
    private const float RetargetInterval = 1.5f;
    private const float ArriveDistance = 1f;

    private float _retargetTimer;

    protected override void OnEnter()
    {
        Context.Mob.Action = MobAction.Fleeing;
        _retargetTimer = 0f;
    }

    protected override void OnUpdate(float deltaTime)
    {
        _retargetTimer -= deltaTime;
        var locomotion = Context.Locomotion;
        if (_retargetTimer > 0f && !locomotion.HasArrived && !locomotion.IsStuck && !locomotion.IsUnreachable) return;

        _retargetTimer = RetargetInterval;
        var position = Context.Mob.Position;
        var away = position - Context.Senses.TargetPosition;
        away = away.LengthSquared > 1e-4f ? away.Normalized() : Context.Mob.Forward;

        var direction = (away + MobRandom.Direction(Context.Random) * Jitter).Normalized();
        locomotion.MoveTo(position + direction * FleeDistance, Context.Definition.FleeSpeed, ArriveDistance);
    }

    protected override void OnExit()
    {
        Context.Locomotion.Stop();
    }
}
