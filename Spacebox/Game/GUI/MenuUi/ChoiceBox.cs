using Engine;
using Engine.CanvasUI;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ChoiceBox : ComboBox
{
    private readonly List<string> _labels = new List<string>();
    private int _selected = -1;
    private bool _updating;

    public ChoiceBox()
    {
        AllowCustomText = false;
        Editable = false;
        ValueChanged += OnValueChanged;
    }

    public event Action<int> IndexChanged;

    private bool _disabled;

    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            MouseFilter = value ? MouseFilter.Ignore : MouseFilter.Stop;
            Modulate = value ? new OpenTK.Mathematics.Color4(1f, 1f, 1f, 0.55f) : OpenTK.Mathematics.Color4.White;
        }
    }

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            int index = _labels.Count == 0 ? -1 : Math.Clamp(value, 0, _labels.Count - 1);
            if (index == _selected && (index < 0 || Value == _labels[index])) return;
            _selected = index;
            _updating = true;
            Value = index >= 0 ? _labels[index] : "";
            _updating = false;
        }
    }

    public void SetChoices(IEnumerable<string> labels, IReadOnlyList<Texture2D> icons = null, bool flipIcons = false)
    {
        _labels.Clear();
        var options = new List<ListItem>();
        int i = 0;
        foreach (var label in labels)
        {
            _labels.Add(label);
            var icon = icons != null && i < icons.Count ? icons[i] : null;
            options.Add(new ListItem { Text = label, Icon = icon, IconRegion = flipIcons && icon != null ? new Rect2(0f, icon.Height, icon.Width, -icon.Height) : null });
            i++;
        }
        SetOptions(options);
        int previous = _selected;
        _selected = -1;
        SelectedIndex = previous < 0 ? 0 : previous;
    }

    private void OnValueChanged(string value)
    {
        if (_updating) return;
        int index = _labels.IndexOf(value);
        if (index < 0 || index == _selected) return;
        _selected = index;
        IndexChanged?.Invoke(index);
    }
}
