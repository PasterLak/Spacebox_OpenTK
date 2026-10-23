using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class MenuCanvas : IDisposable
{
    private const float LogoHeight = 150f;
    private const float LinkSize = 28f;

    private readonly CanvasLayer _layer = new CanvasLayer("Menu");
    private readonly GameMenu _menu;
    private readonly MenuMusicController _music;
    private readonly Control _pageHolder = new CenterContainer { Name = "Pages", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MouseFilter = MouseFilter.Ignore };
    private readonly TextureRect _logo = new TextureRect { Name = "Logo", StretchMode = TextureStretchMode.KeepAspectCentered, FlipV = true, CustomMinimumSize = new Vector2(0f, LogoHeight), MouseFilter = MouseFilter.Ignore };
    private readonly Label _pressAnyKey = new Label { Text = "Press any key to start", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly VBoxContainer _links = new VBoxContainer { Name = "Links" };
    private readonly IconButton _musicButton = new IconButton(LinkSize);
    private readonly Label _version = new Label { Text = $"Version {Application.Version} alpha" };
    private readonly Label _author = new Label { Text = "Made by PasterLak" };
    private SkiaCanvasFont _font;
    private GameUiDesign _design = (GameUiDesign)(-1);
    private float _scale = -1f;
    private MenuPage _page;
    private Type _pageType;
    private float _time;

    public static MenuCanvas Current { get; private set; }

    public bool WantsPointer => _layer.Root.WantsPointer;
    public bool HasPageFor(Type windowType) => _menu.PageOf(windowType) != null;

    public MenuCanvas(GameMenu menu, MenuMusicController music, Texture2D logo)
    {
        Current = this;
        _menu = menu;
        _music = music;
        var root = _layer.Root;
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = GameCanvas.VirtualSize;
        _font = new SkiaCanvasFont(Path.Combine(AppContext.BaseDirectory, GameCanvasThemes.FontPath), (int)GameCanvasThemes.FontSize);
        _logo.Texture = logo;
        Build(root);
        Apply(Settings.Interface);
    }

    private void Build(CanvasRoot root)
    {
        var area = root.Add(new SafeArea { Name = "MenuArea", MouseFilter = MouseFilter.Ignore }, UiLayer.Windows);
        area.SetAnchorsPreset(LayoutPreset.FullRect);
        var column = new VBoxContainer { MouseFilter = MouseFilter.Ignore };
        column.SetAnchorsPreset(LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 12f);
        area.AddChild(column);
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 24f), MouseFilter = MouseFilter.Ignore });
        column.AddChild(_logo);
        column.AddChild(_pageHolder);
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 32f), MouseFilter = MouseFilter.Ignore });

        var hud = root.Add(new SafeArea { Name = "MenuDecor", MouseFilter = MouseFilter.Ignore }, UiLayer.Hud);
        hud.SetAnchorsPreset(LayoutPreset.FullRect);

        _pressAnyKey.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, 160f);
        _pressAnyKey.GrowHorizontal = GrowDirection.Both;
        _pressAnyKey.AddThemeColorOverride("font_color", Color4.White);
        hud.AddChild(_pressAnyKey);

        _links.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, 6f);
        hud.AddChild(_links);
        foreach (var label in new[] { _version, _author }) label.AddThemeColorOverride("font_color", Color4.White);
        _version.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft, 10f);
        _author.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, 10f);
        _author.GrowHorizontal = GrowDirection.Begin;
        hud.AddChild(_version);
        hud.AddChild(_author);

        _musicButton.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight, 6f);
        _musicButton.GrowHorizontal = GrowDirection.Begin;
        _musicButton.Clicked += () =>
        {
            _menu.Click1?.Play();
            _music.Toggle();
        };
        hud.AddChild(_musicButton);
        BuildLinks();
    }

    private void BuildLinks()
    {
        var caption = new Label { Text = "Socials:" };
        caption.AddThemeColorOverride("font_color", Color4.White);
        _links.AddChild(caption);
        foreach (var link in SocialLinks.All())
        {
            var button = new IconButton(LinkSize) { FlipV = true, TooltipText = link.Url };
            button.SetTexture(Resources.Get<Texture2D>(link.IconPath));
            string url = link.Url;
            button.Clicked += () => Application.OpenLink(url);
            _links.AddChild(button);
        }
    }

    public void Apply(InterfaceSettings settings)
    {
        var design = GameUiDesigns.Parse(settings.Design);
        if (design != _design)
        {
            _design = design;
            _layer.Root.Theme = GameCanvasThemes.Create(design, _font);
        }

        float scale = Math.Clamp(settings.UiScale, InterfaceSettings.MinUiScale, InterfaceSettings.MaxUiScale);
        if (MathF.Abs(scale - _scale) < 0.001f) return;
        _scale = scale;
        _layer.Root.Stretch.UiScale = scale;
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        _music.Sync();
        _musicButton.SetSprite(UIAtlas.Get(_music.IsEnabled ? "musicon" : "musicoff"));
        _musicButton.TooltipText = _music.IsEnabled ? "Menu music: on" : "Menu music: off";

        bool menuVisible = GameMenu.IsVisible && !_menu.IsHidden;
        _logo.Visible = CenteredImageMenu.IsVisible;
        _pressAnyKey.Visible = CenteredImageMenu.ShowText && CenteredImageMenu.IsVisible;
        _pressAnyKey.Modulate = new Color4(1f, 1f, 1f, 0.5f * (MathF.Sin(_time * 2f) + 1f));
        _links.Visible = VerticalLinks.IsVisible;
        _version.Visible = CenteredImageMenu.IsVisible;
        _author.Visible = CenteredImageMenu.IsVisible;
        _musicButton.Visible = !_menu.IsHidden;

        SyncPage(menuVisible ? _menu.CurrentWindowType : null);
        _page?.Tick(deltaTime);
        _layer.InputEnabled = !Engine.Debug.IsVisible;
        _layer.Update(deltaTime);
    }

    private void SyncPage(Type windowType)
    {
        var page = windowType != null ? _menu.PageOf(windowType) : null;
        if (windowType == _pageType && page == _page) return;

        if (_page != null) _pageHolder.RemoveChild(_page);
        _pageType = windowType;
        _page = page;
        if (_page == null) return;

        _page.EnsureBuilt();
        _pageHolder.AddChild(_page);
        _page.OnShow();
    }

    private readonly ConfirmDialog _message = new ConfirmDialog();

    public CanvasRoot Root => _layer.Root;

    public void ShowMessage(string title, string text)
    {
        _message.Show(_layer.Root, title ?? "", text ?? "", new[] { "OK" }, _ => { });
    }

    public void Render() => _layer.Render();

    public void Dispose()
    {
        if (Current == this) Current = null;
        _layer.Dispose();
        _font?.Dispose();
        _font = null;
    }
}
