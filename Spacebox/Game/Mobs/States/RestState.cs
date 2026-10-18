using Engine.AI;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Game.Mobs.States;

public sealed class RestState : State<MobContext>
{
    private const float MinPause = 6f;
    private const float MaxPause = 14f;
    private const float DriftDistance = 2.5f;
    private const float ReturnDistance = 10f;
    private const float ArriveDistance = 0.3f;
    private const float DriftSpeedFactor = 0.4f;

    private float _pause;

    protected override void OnEnter()
    {
        Context.Mob.Action = MobAction.Idle;
        Context.Locomotion.Stop();
        _pause = 0f;
    }

    protected override void OnUpdate(float deltaTime)
    {
        _pause -= deltaTime;
        if (_pause > 0f) return;

        _pause = MobRandom.Range(Context.Random, MinPause, MaxPause);

        var grid = Context.Grid;
        var position = Context.Mob.Position;

        if (!SurfaceFinder.IsOnSurface(grid, position))
        {
            if (SurfaceFinder.TryFind(grid, position, 0f, ReturnDistance, Context.Random, out var surface))
                Context.Locomotion.MoveTo(surface, Context.Definition.Speed, ArriveDistance);
            return;
        }

        if (Context.NearestPlayerDistance >= Context.Definition.MealFarDistance) return;

        if (SurfaceFinder.TryFind(grid, position, 1f, DriftDistance, Context.Random, out var goal))
            Context.Locomotion.MoveTo(goal, Context.Definition.Speed * DriftSpeedFactor, ArriveDistance);
    }

    protected override void OnExit()
    {
        Context.Locomotion.Stop();
    }
}
