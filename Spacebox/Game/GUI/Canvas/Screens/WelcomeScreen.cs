using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.MenuUi;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class WelcomeScreen : GameScreen
{
    private const float Width = 400f;
    private const float BannerHeight = 200f;

    private readonly TextureRect _banner = new TextureRect { StretchMode = TextureStretchMode.KeepAspectCovered, CustomMinimumSize = new Vector2(0f, BannerHeight), ClipContents = true };
    private readonly CheckBox _tutorial = new CheckBox("Play the tutorial") { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(0f, 22f) };
    private bool _bannerLoaded;
    private bool _syncing;

    public WelcomeScreen()
    {
        Name = "Welcome";
        var center = AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);

        var frame = MenuLayout.Window(WelcomeUI.Title, Width);
        frame.Root.Name = "WelcomeWindow";
        center.AddChild(frame.Root);
        frame.Body.AddChild(_banner);
        frame.Body.AddChild(new Label { Text = WelcomeUI.Text, Autowrap = true, CustomMinimumSize = new Vector2(Width - 40f, 0f) });
        frame.Body.AddChild(_tutorial);
        _tutorial.Toggled += (_, pressed) =>
        {
            if (!_syncing) WelcomeUI.PlayTutorial = pressed;
        };

        frame.Footer.AddChild(MenuLayout.Spacer());
        var start = frame.Footer.AddChild(MenuLayout.Button(null, "Start", WelcomeUI.Start, false));
        start.CustomMinimumSize = new Vector2(120f, MenuLayout.ButtonHeight);
        frame.Footer.AddChild(MenuLayout.Spacer());
    }

    public override bool IsShown => WelcomeUI.IsVisible;

    protected override bool BlocksPointer => true;

    protected override void Refresh(float deltaTime)
    {
        if (!_bannerLoaded)
        {
            _bannerLoaded = true;
            if (!Application.IsHeadless)
            {
                var texture = MenuTextures.Load(WelcomeUI.BannerPath);
                if (texture != null) texture.FilterMode = FilterMode.Nearest;
                _banner.Texture = texture;
            }
            _banner.Visible = _banner.Texture != null;
        }

        bool hasTutorial = WelcomeUI.HasTutorial;
        if (_tutorial.Visible != hasTutorial) _tutorial.Visible = hasTutorial;
        _syncing = true;
        if (_tutorial.ButtonPressed != WelcomeUI.PlayTutorial) _tutorial.ButtonPressed = WelcomeUI.PlayTutorial;
        _syncing = false;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, new Color4(0f, 0f, 0f, 0.45f));
    }
}
