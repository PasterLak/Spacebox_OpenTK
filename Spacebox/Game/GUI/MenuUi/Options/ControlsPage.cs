using Engine.CanvasUI;
using Engine.InputPro;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ControlsPage : MenuPage
{
    private const float Width = 700f;
    private const float ListHeight = 380f;

    private readonly ControlsWindow _window;
    private readonly VBoxContainer _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly List<BindingRow> _rows = new List<BindingRow>();
    private readonly RemapOverlay _overlay = new RemapOverlay();
    private MenuModal _modal;
    private int _version = -1;

    public ControlsPage(ControlsWindow window)
    {
        _window = window;
    }

    protected override void Build()
    {
        var frame = MenuLayout.Window("Controls", Width);
        AddChild(frame.Root);
        _list.AddThemeConstantOverride("separation", 2f);
        frame.Body.AddChild(MenuLayout.ScrollList(_list, ListHeight));
        frame.Footer.AddChild(OptionsButtons.Save(_window.Options));
        frame.Footer.AddChild(OptionsButtons.Back(_window.Options));

        _modal = new MenuModal(_overlay);
        _modal.Closed += _window.Cancel;
    }

    public override void OnShow()
    {
        _list.ClearChildren();
        _rows.Clear();
        var groups = new SortedDictionary<string, List<(string Id, InputAction Action)>>(StringComparer.Ordinal);
        foreach (var (id, action) in InputManager.Instance.GetAllActions())
        {
            string category = string.IsNullOrEmpty(action.Category) ? "General" : action.Category;
            if (!groups.TryGetValue(category, out var list)) groups[category] = list = new List<(string, InputAction)>();
            list.Add((id, action));
        }

        foreach (var group in groups)
        {
            _list.AddChild(new Label { Text = group.Key.ToUpperInvariant(), ThemeTypeVariation = "SectionLabel" });
            foreach (var (id, action) in group.Value) AddRow(id, action);
        }

        _version = -1;
        Tick(0f);
    }

    private void AddRow(string id, InputAction action)
    {
        var row = new BindingRow(action, () =>
        {
            _window.Options.PlayClick();
            _window.Remap(id);
        });
        _list.AddChild(row);
        _rows.Add(row);
    }

    public override void Tick(float deltaTime)
    {
        SyncOverlay();
        if (_version == _window.BindingsVersion) return;
        _version = _window.BindingsVersion;
        foreach (var row in _rows)
        {
            string text = _window.BindingText(row.Action);
            row.SetBinding(text, _window.IsDuplicate(text));
        }
    }

    private void SyncOverlay()
    {
        if (_window.IsRemapping)
        {
            _overlay.Update(_window.RemapProgress, _window.RemapTimeLeft);
            if (!_modal.IsOpen) _modal.Open(Root);
            return;
        }

        if (!_modal.IsOpen) return;
        _modal.Closed -= _window.Cancel;
        _modal.Close();
        _modal.Closed += _window.Cancel;
    }
}
