using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class DeathScreenView : GameScreen
{
    private const float ButtonWidth = 250f;
    private const float ButtonHeight = 44f;

    private static readonly Color4 MessageColor = new Color4(1f, 0.2f, 0.2f, 1f);
    private static readonly Color4 MessageShadow = new Color4(0f, 0f, 0f, 0.8f);
    private static readonly Color4 CauseColor = new Color4(0.9f, 0.9f, 0.9f, 0.8f);
    private static readonly Color4 CauseShadow = new Color4(0f, 0f, 0f, 0.6f);

    private readonly ParallaxTitle _message = new ParallaxTitle("YOU DIED", 72f, MessageColor, MessageShadow, new Color4(0f, 0f, 0f, 0f), 3f);
    private readonly ParallaxTitle _cause = new ParallaxTitle("", 24f, CauseColor, CauseShadow, new Color4(0f, 0f, 0f, 0f), 2f);
    private readonly VBoxContainer _buttons = new VBoxContainer { Name = "DeathButtons" };
    private readonly Button _respawn = new Button { Text = "Respawn", CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight) };
    private readonly Button _reconstruct = new Button { Text = "Reconstruct from DNA", CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight) };
    private readonly Label _blocker = Windows.StatusText.Create("", Windows.StatusKind.Warning);
    private DeathScreen _screen;

    public DeathScreenView()
    {
        Name = "Death";
        var center = AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        var column = center.AddChild(new VBoxContainer { MouseFilter = MouseFilter.Ignore });
        column.AddThemeConstantOverride("separation", 16f);
        column.AddChild(_message);
        column.AddChild(_cause);
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 60f), MouseFilter = MouseFilter.Ignore });
        var slot = column.AddChild(new VBoxContainer { MouseFilter = MouseFilter.Ignore, CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight * 3f + 24f) });
        slot.AddChild(_buttons);
        _buttons.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _buttons.AddThemeConstantOverride("separation", 12f);
        _buttons.AddChild(_respawn);
        _buttons.AddChild(_reconstruct);
        _buttons.AddChild(_blocker);
        _blocker.HorizontalAlignment = HorizontalAlignment.Center;
        _respawn.Pressed += _ => _screen?.Respawn();
        _reconstruct.Pressed += _ => _screen?.Reconstruct();
    }

    public DeathScreen Screen
    {
        get => _screen;
        set => _screen = value;
    }

    public override bool IsShown => _screen != null && _screen.IsVisible;

    public override bool WantsInput => IsShown;

    protected override bool BlocksPointer => true;

    protected override void Refresh(float deltaTime)
    {
        _message.Text = _screen.DeathMessage;
        _cause.Text = _screen.DeathCause;
        _message.Intensity = _screen.ParallaxIntensity;
        _cause.Intensity = _screen.ParallaxIntensity * 0.8f;
        _message.Tick();
        _cause.Tick();

        bool buttons = _screen.ShowsButtons;
        if (_buttons.Visible != buttons) _buttons.Visible = buttons;
        if (!buttons) return;

        bool canReconstruct = _screen.CanReconstruct;
        string blocker = canReconstruct ? _screen.Blocker : null;
        if (_reconstruct.Visible != canReconstruct) _reconstruct.Visible = canReconstruct;
        if (_reconstruct.Disabled != (blocker != null)) _reconstruct.Disabled = blocker != null;
        SetText(_blocker, blocker);
        if (_blocker.Visible != (blocker != null)) _blocker.Visible = blocker != null;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        if (_screen != null) renderer.DrawRect(LocalRect, _screen.OverlayColor);
    }
}
