using Engine.CanvasUI;
using Spacebox.Editor.Canvas.Pages;
using Spacebox.Editor.Data;
using Spacebox.Editor.Preview;

namespace Spacebox.Editor.Canvas.Preview;

public abstract class PreviewEntryView : EntryView
{
    private readonly VBoxContainer _root = new VBoxContainer { Name = "Preview" };
    private readonly Label _note = new Label { FontColor = EditorCanvasTheme.TextDim };
    private PreviewView _view;

    protected HFlowContainer Toolbar { get; } = new HFlowContainer();
    protected VBoxContainer Extra { get; } = new VBoxContainer();
    protected EditorEntry Entry { get; private set; }
    protected virtual bool ReadsRuntimeAssets => true;

    protected override Control Create()
    {
        _root.AddChild(Toolbar);
        _root.AddChild(_note);
        _root.AddChild(Extra);
        BuildToolbar();
        Toolbar.Visible = Toolbar.Children.Count > 0;
        return _root;
    }

    public override void Show(EditorContext editor, EditorEntry entry)
    {
        Entry = entry;
        if (_view == null)
        {
            _view = new PreviewView(editor.CreateViewport()) { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
            _root.AddChild(_view);
        }
        OnShow(editor, entry);
    }

    public override void Tick(EditorContext editor, float deltaTime)
    {
        if (Entry == null || _view == null) return;
        _note.Text = ReadsRuntimeAssets && Entry.File.IsDirty ? "Save and reload to update the preview." : "";
        _note.Visible = _note.Text.Length > 0;
        Apply(editor, Entry, _view.Viewport);
    }

    protected virtual void BuildToolbar()
    {
    }

    protected virtual void OnShow(EditorContext editor, EditorEntry entry)
    {
    }

    protected abstract void Apply(EditorContext editor, EditorEntry entry, PreviewViewport viewport);

    protected Button AddButton(string text, string tooltip, Action action)
    {
        var button = Toolbar.AddChild(new Button(text) { TooltipText = tooltip, ThemeTypeVariation = "SmallButton" });
        button.Pressed += _ => action();
        return button;
    }

    protected CheckBox AddCheckBox(string text, bool value, string tooltip, Action<bool> changed)
    {
        var box = Toolbar.AddChild(new CheckBox(text, value) { TooltipText = tooltip });
        box.Toggled += (_, pressed) => changed(pressed);
        return box;
    }
}
