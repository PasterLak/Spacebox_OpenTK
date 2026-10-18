using Engine.AI;

namespace Spacebox.Game.Mobs.States;

public sealed class ApproachFoodState : State<MobContext>
{
    private const float ArriveDistance = 0.35f;
    private const float LookDistance = 2.5f;
    private const float ValidateInterval = 0.5f;
    private const float GiveUpTime = 12f;

    private float _validateTimer;

    public bool IsNearFood { get; private set; }
    public bool HasFailed { get; private set; }

    protected override void OnEnter()
    {
        IsNearFood = false;
        HasFailed = false;
        _validateTimer = ValidateInterval;
        Context.Mob.Action = MobAction.Moving;
    }

    protected override void OnUpdate(float deltaTime)
    {
        var food = Context.Food;
        if (!food.IsValid)
        {
            HasFailed = true;
            return;
        }

        _validateTimer -= deltaTime;
        if (_validateTimer <= 0f)
        {
            _validateTimer = ValidateInterval;
            if (!food.IsEdible(Context.Grid, Context.Definition))
            {
                Context.Food = default;
                HasFailed = true;
                return;
            }
        }

        var locomotion = Context.Locomotion;
        locomotion.MoveTo(food.FeedWorld, Context.Definition.Speed, ArriveDistance);

        var blockPosition = food.BlockWorld;
        if ((blockPosition - Context.Mob.Position).LengthSquared < LookDistance * LookDistance)
            Context.Mob.LookAt(blockPosition);

        IsNearFood = locomotion.HasArrived;
        HasFailed = locomotion.IsUnreachable || locomotion.IsStuck || TimeInState > GiveUpTime;
    }

    protected override void OnExit()
    {
        Context.Locomotion.Stop();
    }
}
