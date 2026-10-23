using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Network.Auth;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ConnectionView : CenterContainer
{
    private const int MaxVisibleSteps = 10;

    private static readonly Color4 TitleColor = Color4.White;
    private static readonly Color4 StepColor = new Color4(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Color4 CurrentStepColor = new Color4(1f, 0.85f, 0.1f, 1f);
    private static readonly Color4 ErrorColor = new Color4(1f, 0.3f, 0.3f, 1f);

    private readonly Label _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly VBoxContainer _steps = new VBoxContainer();
    private readonly ProgressBar _progress = new ProgressBar { MinValue = 0f, MaxValue = 1f, ShowPercentage = false, CustomMinimumSize = new Vector2(360f, 10f), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
    private readonly Label _error = new Label { HorizontalAlignment = HorizontalAlignment.Center, Autowrap = true, CustomMinimumSize = new Vector2(560f, 0f) };
    private readonly Button _button = new Button { CustomMinimumSize = new Vector2(180f, 32f), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
    private int _shownSteps = -1;
    private string _shownError;

    public ConnectionView()
    {
        Name = "Connection";
        MouseFilter = MouseFilter.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        var column = new VBoxContainer { MouseFilter = MouseFilter.Ignore, CustomMinimumSize = new Vector2(600f, 0f) };
        column.AddThemeConstantOverride("separation", 10f);
        _title.AddThemeColorOverride("font_color", TitleColor);
        _title.AddThemeFontSizeOverride(26f);
        _error.AddThemeColorOverride("font_color", ErrorColor);
        _steps.AddThemeConstantOverride("separation", 2f);
        column.AddChild(_title);
        column.AddChild(_steps);
        column.AddChild(_progress);
        column.AddChild(_error);
        column.AddChild(_button);
        _button.Pressed += _ => ConnectionScreen.Leave();
        AddChild(column);
    }

    public void Sync()
    {
        _title.Text = ConnectionScreen.Title ?? "";
        var steps = ConnectionScreen.Steps;
        string error = ConnectionScreen.Error;
        if (steps.Count != _shownSteps || error != _shownError) RebuildSteps(steps, error);

        var progress = ConnectionScreen.Progress;
        _progress.Visible = progress.HasValue;
        if (progress.HasValue) _progress.Value = progress.Value;
        _error.Text = error ?? "";
        _error.Visible = error != null;
        _button.Text = error != null ? "Back to menu" : "Cancel";
        _button.Visible = !ClientAuth.IsAwaitingPassword;
    }

    private void RebuildSteps(IReadOnlyList<string> steps, string error)
    {
        _shownSteps = steps.Count;
        _shownError = error;
        _steps.ClearChildren();
        int first = Math.Max(0, steps.Count - MaxVisibleSteps);
        for (int i = first; i < steps.Count; i++)
        {
            bool current = i == steps.Count - 1 && error == null;
            var label = new Label { Text = steps[i], HorizontalAlignment = HorizontalAlignment.Center };
            label.AddThemeColorOverride("font_color", current ? CurrentStepColor : StepColor);
            _steps.AddChild(label);
        }
    }
}
