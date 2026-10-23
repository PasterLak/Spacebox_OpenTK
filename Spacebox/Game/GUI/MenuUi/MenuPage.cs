using Engine.CanvasUI;

namespace Spacebox.Game.GUI.MenuUi;

public abstract class MenuPage : CenterContainer
{
    private bool _built;

    protected MenuPage()
    {
        MouseFilter = MouseFilter.Ignore;
    }

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        Build();
    }

    public virtual bool ShowsLogo => true;

    protected abstract void Build();

    public virtual void OnShow()
    {
    }

    public virtual void Tick(float deltaTime)
    {
    }
}
