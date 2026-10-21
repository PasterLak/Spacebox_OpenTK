namespace Engine.Automation;

public sealed record UiTextRun(string Text, float X, float Y, float Width, float Height)
{
    public float CenterX => X + Width * 0.5f;
    public float CenterY => Y + Height * 0.5f;
}

public sealed record UiWindowText(string Name, string Id, int Layer, float X, float Y, float Width, float Height, IReadOnlyList<UiTextRun> Texts);

public sealed record UiTextSnapshot(float DisplayWidth, float DisplayHeight, IReadOnlyList<UiWindowText> Windows);
