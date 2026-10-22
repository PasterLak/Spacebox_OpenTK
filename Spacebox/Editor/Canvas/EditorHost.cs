using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Editor.Canvas;

public sealed class EditorHost : IDisposable
{
    private readonly EditorContext _context;
    private readonly List<EditorPage> _pages;
    private readonly CanvasLayer _layer = new CanvasLayer("GameSet editor");
    private readonly List<Button> _navButtons = new List<Button>();
    private readonly Control _pageHolder = new Control { Name = "PageHolder", MouseFilter = MouseFilter.Ignore };
    private readonly Label _title = new Label();
    private readonly Label _status = new Label { Overrun = TextOverrun.Ellipsis };
    private readonly Label _dirty = new Label();
    private readonly ConfirmDialog _dialog = new ConfirmDialog();
    private Button _save;
    private Button _discard;
    private SkiaCanvasFont _font;
    private int _current = -1;

    public EditorHost(EditorContext context, List<EditorPage> pages, string startPage)
    {
        _context = context;
        _pages = pages;
        _font = new SkiaCanvasFont(Path.Combine(AppContext.BaseDirectory, EditorCanvasTheme.FontPath), (int)EditorCanvasTheme.FontSize);
        var root = _layer.Root;
        root.Theme = EditorCanvasTheme.Create(_font);
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = new Vector2(1280f, 720f);
        root.Stretch.ScaleStep = 0.5f;
        root.Stretch.MinScale = 1f;
        root.UnhandledKeyPress += OnShortcut;
        Build(root);

        foreach (var page in _pages) page.Attach(context);
        int start = _pages.FindIndex(p => string.Equals(p.Title, startPage, StringComparison.OrdinalIgnoreCase));
        Select(start < 0 ? 0 : start);
    }

    public CanvasRoot Root => _layer.Root;
    public EditorPage CurrentPage => _current >= 0 ? _pages[_current] : null;
    public bool WantsPointer => _layer.Root.WantsPointer;

    public void Select(int index)
    {
        if (index < 0 || index >= _pages.Count || index == _current) return;

        if (_current >= 0)
        {
            _pages[_current].Hide();
            _pageHolder.RemoveChild(_pages[_current]);
        }

        _current = index;
        var page = _pages[index];
        _pageHolder.AddChild(page);
        page.SetAnchorsPreset(LayoutPreset.FullRect);
        page.Show();
        for (int i = 0; i < _navButtons.Count; i++) _navButtons[i].ButtonPressed = i == index;
    }

    public void Select(string title)
    {
        Select(_pages.FindIndex(p => string.Equals(p.Title, title, StringComparison.OrdinalIgnoreCase)));
    }

    public void Update(float deltaTime)
    {
        int dirty = _context.Project.DirtyCount;
        _save.Text = dirty > 0 ? $"Save ({dirty})" : "Save";
        _save.Disabled = dirty == 0;
        _discard.Disabled = dirty == 0;
        _dirty.Text = dirty > 0 ? $"{dirty} unsaved file(s)" : "All changes saved";
        _dirty.FontColor = dirty > 0 ? EditorCanvasTheme.Warning : EditorCanvasTheme.TextDim;
        _status.Text = _context.StatusText;
        _status.FontColor = _context.StatusIsError ? EditorCanvasTheme.Error : EditorCanvasTheme.Success;

        CurrentPage?.Tick(deltaTime);
        _layer.Update(deltaTime);
    }

    public void Render() => _layer.Render();

    public void Dispose()
    {
        foreach (var page in _pages) page.Release();
        _layer.Dispose();
        _font?.Dispose();
        _font = null;
    }

    private void Build(CanvasRoot root)
    {
        var background = root.Add(new ColorRect { Color = EditorCanvasTheme.Background, MouseFilter = MouseFilter.Ignore }, UiLayer.Background);
        background.SetAnchorsPreset(LayoutPreset.FullRect);

        var main = root.Add(new VBoxContainer { Name = "EditorMain" }, UiLayer.Windows);
        main.SetAnchorsPreset(LayoutPreset.FullRect);
        main.SetOffsets(6f, 4f, -6f, -4f);

        var toolbar = main.AddChild(new HBoxContainer { Name = "Toolbar" });
        _title.Text = $"GameSet Editor - {_context.Assets.ModId}  ({_context.Project.FolderName})";
        _title.FontColor = EditorCanvasTheme.Accent;
        _title.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        toolbar.AddChild(_title);
        toolbar.AddChild(new VSeparator());
        _save = Toolbar(toolbar, "Save", "Saves every changed file (Ctrl+S).", Save);
        Toolbar(toolbar, "Save & Reload", "Saves all files and reloads the GameSet (textures, atlases, previews).", () => { Save(); _context.RequestReload(); });
        Toolbar(toolbar, "Reload", "Reloads textures and data from disk without saving.", _context.RequestReload);
        _discard = Toolbar(toolbar, "Discard", "Throws away all unsaved changes and reloads the files from disk.", Discard);
        var spacer = toolbar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, MouseFilter = MouseFilter.Ignore });
        spacer.CustomMinimumSize = new Vector2(10f, 0f);
        Toolbar(toolbar, "Back to menu", "Leave the editor. Asks first if there are unsaved changes.", RequestExit);
        main.AddChild(new HSeparator());

        var split = main.AddChild(new SplitContainer { Name = "Body", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 110f, MinSecond = 400f });
        split.Ratio = 0.12f;
        var nav = split.AddChild(new VBoxContainer { Name = "Navigation" });
        nav.AddThemeConstantOverride("separation", 1f);
        for (int i = 0; i < _pages.Count; i++)
        {
            int index = i;
            var button = nav.AddChild(new Button(_pages[i].Title) { ThemeTypeVariation = "NavButton", Alignment = HorizontalAlignment.Left, ToggleMode = true, TooltipText = _pages[i].Description });
            button.Pressed += _ =>
            {
                Select(index);
                button.ButtonPressed = true;
            };
            _navButtons.Add(button);
        }
        split.AddChild(_pageHolder);

        main.AddChild(new HSeparator());
        var statusBar = main.AddChild(new HBoxContainer { Name = "StatusBar" });
        statusBar.AddChild(_dirty);
        statusBar.AddChild(new VSeparator());
        _status.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        statusBar.AddChild(_status);
        if (_context.Project.Mirror != null)
            statusBar.AddChild(new Label("Development build: saves also go to the source GameSet") { FontColor = EditorCanvasTheme.TextDim, TooltipText = _context.Project.Mirror.Root, MouseFilter = MouseFilter.Stop });
    }

    private static Button Toolbar(Container parent, string text, string tooltip, Action action)
    {
        var button = parent.AddChild(new Button(text) { TooltipText = tooltip });
        button.Pressed += _ => action();
        return button;
    }

    private void OnShortcut(KeyPress press)
    {
        if (press.Control && press.Key == Keys.S) Save();
    }

    private void Save()
    {
        try
        {
            _context.Project.SaveAll();
            _context.SetStatus("Saved");
        }
        catch (Exception ex)
        {
            _context.SetStatus("Save failed: " + ex.Message, true);
        }
    }

    private void Discard()
    {
        _dialog.Show(Root, "Discard changes", $"Throw away {_context.Project.DirtyCount} unsaved file(s)?", new[] { "Discard", "Cancel" }, result =>
        {
            if (result != 0) return;
            _context.Project.DiscardChanges();
            _context.SetStatus("Changes discarded");
        });
    }

    private void RequestExit()
    {
        int dirty = _context.Project.DirtyCount;
        if (dirty == 0)
        {
            _context.RequestExit();
            return;
        }

        _dialog.Show(Root, "Unsaved changes", $"{dirty} file(s) have unsaved changes.", new[] { "Save and exit", "Exit without saving", "Cancel" }, result =>
        {
            if (result == 0)
            {
                Save();
                _context.RequestExit();
            }
            else if (result == 1)
            {
                _context.Project.DiscardChanges();
                _context.RequestExit();
            }
        });
    }
}
