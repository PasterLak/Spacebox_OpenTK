using Engine.AI;

namespace Spacebox.Game.Mobs.States;

public sealed class ReturnHomeState : State<MobContext>
{
    private const float ArriveDistance = 1.5f;
    private const float RetargetInterval = 3f;

    private float _retargetTimer;

    public bool IsDone { get; private set; }

    protected override void OnEnter()
    {
        Context.Mob.Action = MobAction.Moving;
        IsDone = false;
        _retargetTimer = 0f;
    }

    protected override void OnUpdate(float deltaTime)
    {
        var locomotion = Context.Locomotion;
        if (!Context.IsFarFromHome || locomotion.IsUnreachable)
        {
            IsDone = true;
            return;
        }

        _retargetTimer -= deltaTime;
        if (_retargetTimer > 0f && !locomotion.HasArrived && !locomotion.IsStuck) return;

        _retargetTimer = RetargetInterval;
        locomotion.MoveTo(Context.Home, Context.Definition.Speed, ArriveDistance);
    }

    protected override void OnExit()
    {
        Context.Locomotion.Stop();
    }
}
