
namespace Engine.InputPro;

public class CompositeBinding : InputBinding
{
    public List<InputBinding> Bindings { get; set; } = new();
    public bool RequireAll { get; set; }

    public CompositeBinding() { }
    public CompositeBinding(bool requireAll, params InputBinding[] bindings)
    {
        RequireAll = requireAll;
        Bindings.AddRange(bindings);
    }

    public override bool IsPressed(InputState state)
    {
        if (RequireAll)
            return Bindings.All(b => b.IsHeld(state)) && Bindings.Any(b => b.IsPressed(state));
        return Bindings.Any(b => b.IsPressed(state));
    }

    public override bool IsReleased(InputState state)
    {
        if (RequireAll)
            return Bindings.Any(b => b.IsReleased(state));
        return Bindings.Any(b => b.IsReleased(state));
    }

    public override bool IsHeld(InputState state)
    {
        if (RequireAll)
            return Bindings.All(b => b.IsHeld(state));
        return Bindings.Any(b => b.IsHeld(state));
    }

    public override string GetDisplayName()
    {
        var names = Bindings.Select(b => b.GetDisplayName());
        return RequireAll ? string.Join(" + ", names) : string.Join(" / ", names);
    }
}
