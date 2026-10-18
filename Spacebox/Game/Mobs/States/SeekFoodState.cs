using Engine.AI;

namespace Spacebox.Game.Mobs.States;

public sealed class SeekFoodState : State<MobContext>
{
    private const float SearchDuration = 0.4f;

    protected override void OnEnter()
    {
        Context.Food = default;
        Context.FoodSearchFailed = false;
        Context.Mob.Action = MobAction.Idle;
        Context.Locomotion.Stop();
        Context.FoodFinder.Begin(Context.Grid, Context.Definition, Context.Mob.Position);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (Context.Food.IsValid || Context.FoodSearchFailed) return;

        Context.FoodFinder.Step();
        if (TimeInState < SearchDuration) return;

        var food = Context.FoodFinder.Result;
        if (food.IsValid) Context.Food = food;
        else Context.FoodSearchFailed = true;
    }
}
