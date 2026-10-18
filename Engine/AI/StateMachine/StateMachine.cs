namespace Engine.AI;

public class StateMachine<TContext> : State<TContext> where TContext : class
{
    private static readonly List<Transition> NoTransitions = new List<Transition>();

    private readonly List<State<TContext>> _states = new List<State<TContext>>();
    private readonly List<Transition> _anyTransitions = new List<Transition>();
    private readonly Dictionary<State<TContext>, List<Transition>> _transitions = new Dictionary<State<TContext>, List<Transition>>();
    private List<Transition> _currentTransitions = NoTransitions;
    private State<TContext>? _initial;
    private bool _isBound;

    public State<TContext>? Current { get; private set; }
    public IReadOnlyList<State<TContext>> States => _states;

    public event Action<State<TContext>?, State<TContext>>? StateChanged;

    public StateMachine()
    {
    }

    public StateMachine(TContext context)
    {
        Bind(context);
    }

    public TState Add<TState>(TState state) where TState : State<TContext>
    {
        if (_states.Contains(state)) return state;

        _states.Add(state);
        _initial ??= state;
        if (_isBound) state.Bind(Context);
        return state;
    }

    public StateMachine<TContext> SetInitial(State<TContext> state)
    {
        Add(state);
        _initial = state;
        return this;
    }

    public StateMachine<TContext> AddTransition(State<TContext> from, State<TContext> to, Func<TContext, bool> condition)
    {
        Add(from);
        Add(to);

        if (!_transitions.TryGetValue(from, out var list))
        {
            list = new List<Transition>();
            _transitions[from] = list;
        }

        list.Add(new Transition(to, condition));
        if (Current == from) _currentTransitions = list;
        return this;
    }

    public StateMachine<TContext> AddAnyTransition(State<TContext> to, Func<TContext, bool> condition)
    {
        Add(to);
        _anyTransitions.Add(new Transition(to, condition));
        return this;
    }

    public void Update(float deltaTime)
    {
        if (!IsActive) Enter();
        Tick(deltaTime);
    }

    public void Stop()
    {
        if (IsActive) Exit();
    }

    public void ChangeState(State<TContext> next)
    {
        if (next == null || next == Current) return;

        var previous = Current;
        previous?.Exit();

        Current = next;
        _currentTransitions = _transitions.TryGetValue(next, out var list) ? list : NoTransitions;
        next.Enter();

        StateChanged?.Invoke(previous, next);
    }

    public bool IsIn(State<TContext> state)
    {
        if (Current == state) return true;
        return Current is StateMachine<TContext> nested && nested.IsIn(state);
    }

    protected override void OnBind()
    {
        _isBound = true;
        for (int i = 0; i < _states.Count; i++) _states[i].Bind(Context);
    }

    protected override void OnEnter()
    {
        Current = null;
        _currentTransitions = NoTransitions;
        if (_initial != null) ChangeState(_initial);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (TryGetTransition(out var next)) ChangeState(next);
        Current?.Tick(deltaTime);
    }

    protected override void OnExit()
    {
        Current?.Exit();
        Current = null;
        _currentTransitions = NoTransitions;
    }

    private bool TryGetTransition(out State<TContext> next)
    {
        for (int i = 0; i < _anyTransitions.Count; i++)
        {
            var transition = _anyTransitions[i];
            if (transition.To != Current && transition.Condition(Context))
            {
                next = transition.To;
                return true;
            }
        }

        for (int i = 0; i < _currentTransitions.Count; i++)
        {
            var transition = _currentTransitions[i];
            if (transition.Condition(Context))
            {
                next = transition.To;
                return true;
            }
        }

        next = null!;
        return false;
    }

    private readonly struct Transition
    {
        public readonly State<TContext> To;
        public readonly Func<TContext, bool> Condition;

        public Transition(State<TContext> to, Func<TContext, bool> condition)
        {
            To = to;
            Condition = condition;
        }
    }
}
