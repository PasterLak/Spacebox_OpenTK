using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas.Screens;

public abstract class GameScreen : ScreenOverlay
{
    public abstract bool IsShown { get; }

    public virtual bool WantsInput => false;

    protected virtual bool BlocksPointer => false;

    public void Sync(float deltaTime)
    {
        bool shown = IsShown;
        if (Visible != shown) Visible = shown;
        if (shown) Refresh(deltaTime);
    }

    protected virtual void Refresh(float deltaTime)
    {
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button) => BlocksPointer;

    protected static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text ?? "";
    }
}
