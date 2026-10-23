using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Generation.Blocks.Components;

public abstract class BlockComponent
{
    public ComponentBlock Host { get; private set; }

    public virtual int UsePriority => 0;
    public virtual bool ConsumesPower => false;
    public virtual bool GeneratesPower => false;

    internal void Attach(ComponentBlock host)
    {
        Host = host;
        OnAttached();
    }

    protected virtual void OnAttached()
    {
    }

    public virtual void OnAddedToEntity()
    {
    }

    public virtual void OnRemovedFromEntity()
    {
    }

    public virtual void OnActiveStateChanged(bool isActive)
    {
    }

    public virtual void TickElectric()
    {
    }

    public virtual void ConfigureAsMultiblockPart()
    {
    }

    public virtual void OnDestroyed(HitInfo hit)
    {
    }

    public virtual void Use(LocalAstronaut player, ref HitInfo hit)
    {
    }
}
