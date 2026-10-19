using Spacebox.Game.Player;

namespace Spacebox.Game.Tutorial.Tasks;

public interface ITutorialTask
{
    TutorialTaskDefinition Definition { get; }
    float Current { get; }
    float Target { get; }
    bool IsDone { get; }
    bool ShowsCounter { get; }

    void Start(ITutorialContext context);
    void Update(ITutorialContext context, float deltaTime);
    void OnActivity(in PlayerActivityEvent activity);
    void Restore(float current);
}

public abstract class TutorialTask : ITutorialTask
{
    private const float CompletionTolerance = 0.0005f;

    protected TutorialTask(TutorialTaskDefinition definition, float target)
    {
        Definition = definition;
        Target = MathF.Max(target, 0.001f);
    }

    public TutorialTaskDefinition Definition { get; }
    public float Current { get; private set; }
    public float Target { get; }
    public bool IsDone { get; private set; }
    public virtual bool ShowsCounter => Target > 1f;

    public virtual void Start(ITutorialContext context)
    {
    }

    public virtual void Update(ITutorialContext context, float deltaTime)
    {
    }

    public virtual void OnActivity(in PlayerActivityEvent activity)
    {
    }

    public virtual void Restore(float current)
    {
        SetProgress(current);
    }

    protected void SetProgress(float value)
    {
        if (IsDone) return;

        Current = Math.Clamp(value, 0f, Target);
        if (Current < Target - Target * CompletionTolerance) return;

        Current = Target;
        IsDone = true;
    }

    protected void AddProgress(float amount)
    {
        if (amount > 0f) SetProgress(Current + amount);
    }
}
