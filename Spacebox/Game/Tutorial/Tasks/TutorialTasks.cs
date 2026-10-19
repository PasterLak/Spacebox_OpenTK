using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Tutorial.Tasks;

public sealed class ActivityCountTask : TutorialTask
{
    private readonly PlayerActivityKind _kind;
    private readonly short _itemId;
    private readonly string _tag;

    public ActivityCountTask(TutorialTaskDefinition definition, PlayerActivityKind kind, string tag = null)
        : base(definition, Math.Max(1, definition.Data.Count))
    {
        _kind = kind;
        _itemId = definition.ItemId;
        _tag = string.IsNullOrWhiteSpace(tag) ? null : tag;
    }

    public override void OnActivity(in PlayerActivityEvent activity)
    {
        if (activity.Kind != _kind) return;
        if (_itemId != 0 && activity.Id != _itemId) return;
        if (_tag != null && !string.Equals(activity.Tag, _tag, StringComparison.OrdinalIgnoreCase)) return;

        AddProgress(activity.Count);
    }
}

public sealed class HaveItemTask : TutorialTask
{
    public HaveItemTask(TutorialTaskDefinition definition) : base(definition, Math.Max(1, definition.Data.Count))
    {
    }

    public override void Update(ITutorialContext context, float deltaTime)
    {
        SetProgress(context.CountItem(Definition.ItemId));
    }
}

public sealed class PressTask : TutorialTask
{
    private readonly string _action;

    public PressTask(TutorialTaskDefinition definition) : base(definition, Math.Max(1, definition.Data.Count))
    {
        _action = definition.Data.Action;
    }

    public override void Update(ITutorialContext context, float deltaTime)
    {
        if (context.IsActionPressed(_action)) AddProgress(1f);
    }
}

public sealed class HoldTask : TutorialTask
{
    private readonly string _action;

    public HoldTask(TutorialTaskDefinition definition) : base(definition, definition.Data.Seconds)
    {
        _action = definition.Data.Action;
    }

    public override bool ShowsCounter => false;

    public override void Update(ITutorialContext context, float deltaTime)
    {
        if (context.IsActionHeld(_action)) AddProgress(deltaTime);
    }
}

public sealed class WaitTask : TutorialTask
{
    public WaitTask(TutorialTaskDefinition definition) : base(definition, definition.Data.Seconds)
    {
    }

    public override bool ShowsCounter => false;

    public override void Update(ITutorialContext context, float deltaTime)
    {
        AddProgress(deltaTime);
    }
}

public sealed class StatDeltaTask : TutorialTask
{
    private readonly string _stat;
    private double _baseline;
    private float _offset;
    private bool _started;

    public StatDeltaTask(TutorialTaskDefinition definition, string stat, float target) : base(definition, target)
    {
        _stat = stat;
    }

    public override void Start(ITutorialContext context)
    {
        _baseline = context.GetStat(_stat);
        _offset = Current;
        _started = true;
    }

    public override void Update(ITutorialContext context, float deltaTime)
    {
        if (!_started) Start(context);
        SetProgress(_offset + (float)(context.GetStat(_stat) - _baseline));
    }
}

public sealed class LookTask : TutorialTask
{
    private const float MaxDegreesPerFrame = 45f;

    private Vector3 _last;
    private bool _hasLast;

    public LookTask(TutorialTaskDefinition definition) : base(definition, definition.Data.Degrees)
    {
    }

    public override bool ShowsCounter => false;

    public override void Update(ITutorialContext context, float deltaTime)
    {
        var direction = context.ViewDirection;
        if (direction.LengthSquared < 1e-6f) return;

        direction.Normalize();
        if (_hasLast)
        {
            float dot = Math.Clamp(Vector3.Dot(_last, direction), -1f, 1f);
            float degrees = MathHelper.RadiansToDegrees(MathF.Acos(dot));
            AddProgress(MathF.Min(degrees, MaxDegreesPerFrame));
        }

        _last = direction;
        _hasLast = true;
    }
}

public sealed class ConditionTask : TutorialTask
{
    private readonly Func<ITutorialContext, bool> _condition;

    public ConditionTask(TutorialTaskDefinition definition, Func<ITutorialContext, bool> condition) : base(definition, 1f)
    {
        _condition = condition;
    }

    public override void Update(ITutorialContext context, float deltaTime)
    {
        if (_condition(context)) SetProgress(1f);
    }
}

public sealed class CompositeTask : TutorialTask
{
    private readonly IReadOnlyList<ITutorialTask> _children;
    private readonly bool _requireAll;

    public CompositeTask(TutorialTaskDefinition definition, IReadOnlyList<ITutorialTask> children, bool requireAll)
        : base(definition, requireAll ? Math.Max(1, children.Count) : 1)
    {
        _children = children;
        _requireAll = requireAll;
    }

    public IReadOnlyList<ITutorialTask> Children => _children;

    public override void Start(ITutorialContext context)
    {
        for (int i = 0; i < _children.Count; i++) _children[i].Start(context);
    }

    public override void Update(ITutorialContext context, float deltaTime)
    {
        for (int i = 0; i < _children.Count; i++)
        {
            if (!_children[i].IsDone) _children[i].Update(context, deltaTime);
        }
        Evaluate();
    }

    public override void OnActivity(in PlayerActivityEvent activity)
    {
        for (int i = 0; i < _children.Count; i++)
        {
            if (!_children[i].IsDone) _children[i].OnActivity(activity);
        }
        Evaluate();
    }

    public override void Restore(float current)
    {
        int done = (int)current;
        for (int i = 0; i < _children.Count && i < done; i++)
            _children[i].Restore(_children[i].Target);
        Evaluate();
    }

    private void Evaluate()
    {
        int done = 0;
        for (int i = 0; i < _children.Count; i++)
        {
            if (_children[i].IsDone) done++;
        }

        SetProgress(_requireAll ? done : Math.Min(done, 1));
    }
}
