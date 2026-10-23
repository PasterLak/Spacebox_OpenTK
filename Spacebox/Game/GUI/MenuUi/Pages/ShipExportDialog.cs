using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Ships.Sharing;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ShipExportDialog
{
    private readonly GameMenu _menu;
    private readonly ShipViewerSession _session;
    private readonly MenuModal _modal;
    private readonly LineEdit _name = new LineEdit { MaxLength = 48 };
    private readonly LineEdit _author = new LineEdit { MaxLength = 32 };
    private readonly TextEdit _description = new TextEdit { MaxLength = 400, VisibleLinesMinimum = 4 };
    private readonly Label _file = new Label { ThemeTypeVariation = "MutedLabel" };

    public ShipExportDialog(GameMenu menu, ShipViewerSession session)
    {
        _menu = menu;
        _session = session;
        var frame = MenuLayout.Window("Export ship", 420f);
        frame.Body.AddChild(MenuLayout.Field("Name", _name));
        frame.Body.AddChild(MenuLayout.Field("Author", _author));
        frame.Body.AddChild(MenuLayout.Field("Description", _description));
        frame.Body.AddChild(_file);
        _name.TextChanged += _ => UpdateFile();
        frame.Footer.AddChild(MenuLayout.Button(menu, "Export", Export));
        frame.Footer.AddChild(MenuLayout.Button(menu, "Cancel", Close));
        _modal = new MenuModal(frame.Root);
    }

    public void Open()
    {
        var ships = _session.Ships;
        int index = ships?.Ships.IndexOf(_session.SelectedShip) ?? -1;
        _name.Text = index >= 0 ? ships.Label(index) : "Ship";
        _author.Text = ClientConfigStore.Load()?.PlayerNickname ?? "";
        _description.Text = "";
        UpdateFile();
        _modal.Open(MenuCanvas.Current?.Root);
        _name.GrabFocus();
    }

    public void Close() => _modal.Close();

    private void UpdateFile() => _file.Text = $"File: Ships/{ShipFileNames.MakeId(_name.Text)}.ship";

    private void Export()
    {
        _session.Export(_name.Text, _author.Text, _description.Text);
        Close();
    }
}
