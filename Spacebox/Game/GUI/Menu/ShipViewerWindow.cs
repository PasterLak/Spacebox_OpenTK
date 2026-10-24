using Engine;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Game.GUI.Menu;

public sealed class ShipViewerWindow : MenuWindow, IDisposable
{
    private readonly GameMenu _menu;
    private readonly ShipViewerSession _session;

    public ShipViewerWindow(GameMenu menu)
    {
        _menu = menu;
        string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.Menu.WorldsFolder);
        _session = new ShipViewerSession(worldsDirectory, VersionConverter.Registry, Application.Version);
    }

    public ShipViewerSession Session => _session;

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.ShipViewerPage(_menu, _session);

    public override void OnWindowChanged()
    {
        _session.Open();
    }

    public override void OnWindowClosed()
    {
        _session.Close();
    }

    public override void OnSceneRender()
    {
        _session.Update(Time.Delta);
        _session.Render();
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
