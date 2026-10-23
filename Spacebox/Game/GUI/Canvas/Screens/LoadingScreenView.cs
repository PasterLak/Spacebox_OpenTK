using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class LoadingScreenView : GameScreen
{
    private static readonly Color4 BarBack = new Color4(0.1f, 0.1f, 0.12f, 0.85f);
    private static readonly Color4 BarFill = new Color4(1f, 0.85f, 0.1f, 1f);
    private static readonly Color4 Shadow = new Color4(0f, 0f, 0f, 0.8f);

    private readonly Label _step = new Label { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly ProgressBar _bar = new ProgressBar { MaxValue = 1f, CustomMinimumSize = new Vector2(320f, 8f), ShowPercentage = false };

    public LoadingScreenView()
    {
        Name = "Loading";
        var center = AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        var column = center.AddChild(new VBoxContainer { MouseFilter = MouseFilter.Ignore });
        column.AddThemeConstantOverride("separation", 10f);
        var title = column.AddChild(new Label { Text = WorldLoadingScreen.Title, HorizontalAlignment = HorizontalAlignment.Center, Bold = true });
        title.AddThemeFontSizeOverride(24f);
        title.AddThemeColorOverride("font_color", Color4.White);
        title.ShadowColor = Shadow;
        column.AddChild(_bar);
        _bar.FillColor = BarFill;
        _bar.AddThemeStyleBoxOverride("background", new StyleBoxFlat(BarBack).SetBorder(1f, new Color4(BarFill.R, BarFill.G, BarFill.B, 0.6f)));
        column.AddChild(_step);
        _step.AddThemeColorOverride("font_color", new Color4(0.85f, 0.85f, 0.85f, 1f));
        _step.ShadowColor = Shadow;
    }

    public override bool IsShown => WorldLoadingScreen.IsActive;

    protected override void Refresh(float deltaTime)
    {
        _bar.Value = WorldLoadingScreen.Progress;
        SetText(_step, WorldLoadingScreen.Step);
    }
}
