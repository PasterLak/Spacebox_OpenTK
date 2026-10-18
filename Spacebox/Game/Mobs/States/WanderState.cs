using Engine.AI;
using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs.States;

public sealed class WanderState : State<MobContext>
{
    private const int GoalAttempts = 8;
    private const float MinDistance = 3f;
    private const float MaxDistance = 9f;
    private const float ArriveDistance = 0.6f;
    private const float MaxLegTime = 8f;

    private float _legTime;

    protected override void OnEnter()
    {
        Context.Mob.Action = MobAction.Moving;
        PickGoal();
    }

    protected override void OnUpdate(float deltaTime)
    {
        _legTime += deltaTime;

        var locomotion = Context.Locomotion;
        if (locomotion.HasArrived || locomotion.IsUnreachable || locomotion.IsStuck || _legTime > MaxLegTime)
            PickGoal();
    }

    protected override void OnExit()
    {
        Context.Locomotion.Stop();
    }

    private void PickGoal()
    {
        _legTime = 0f;
        var grid = Context.Grid;
        var origin = Context.Mob.Position;

        for (int i = 0; i < GoalAttempts; i++)
        {
            var goal = origin + MobRandom.Direction(Context.Random) * MobRandom.Range(Context.Random, MinDistance, MaxDistance);
            if (!grid.IsPassable(grid.WorldToCell(goal))) continue;

            Context.Locomotion.MoveTo(goal, Context.Definition.Speed, ArriveDistance);
            return;
        }

        Context.Locomotion.Stop();
    }
}
