namespace Engine.AI;

public abstract class State<TContext> where TContext : class
{
    public TContext Context { get; private set; } = null!;
    public float TimeInState { get; private set; }
    public bool IsActive { get; private set; }
    public virtual string Name => GetType().Name;

    internal void Bind(TContext context)
    {
        Context = context;
        OnBind();
    }

    internal void Enter()
    {
        TimeInState = 0f;
        IsActive = true;
        OnEnter();
    }

    internal void Tick(float deltaTime)
    {
        TimeInState += deltaTime;
        OnUpdate(deltaTime);
    }

    internal void Exit()
    {
        IsActive = false;
        OnExit();
    }

    protected virtual void OnBind()
    {
    }

    protected virtual void OnEnter()
    {
    }

    protected virtual void OnUpdate(float deltaTime)
    {
    }

    protected virtual void OnExit()
    {
    }
}
