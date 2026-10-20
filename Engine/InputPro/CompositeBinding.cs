
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
            return AllHeld(state) && AnyPressed(state);
        return AnyPressed(state);
    }

    public override bool IsReleased(InputState state)
    {
        for (int i = 0; i < Bindings.Count; i++)
        {
            if (Bindings[i].IsReleased(state)) return true;
        }
        return false;
    }

    public override bool IsHeld(InputState state)
    {
        if (RequireAll)
            return AllHeld(state);

        for (int i = 0; i < Bindings.Count; i++)
        {
            if (Bindings[i].IsHeld(state)) return true;
        }
        return false;
    }

    private bool AllHeld(InputState state)
    {
        for (int i = 0; i < Bindings.Count; i++)
        {
            if (!Bindings[i].IsHeld(state)) return false;
        }
        return true;
    }

    private bool AnyPressed(InputState state)
    {
        for (int i = 0; i < Bindings.Count; i++)
        {
            if (Bindings[i].IsPressed(state)) return true;
        }
        return false;
    }

    public override string GetDisplayName()
    {
        var names = Bindings.Select(b => b.GetDisplayName());
        return RequireAll ? string.Join(" + ", names) : string.Join(" / ", names);
    }
}
