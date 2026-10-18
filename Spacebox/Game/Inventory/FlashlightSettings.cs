namespace Spacebox.Game;

public sealed class FlashlightSettings
{
    public static FlashlightSettings Default { get; } = new FlashlightSettings();

    public float Intensity { get; set; } = 1f;
    public float Range { get; set; } = 50f;
    public float InnerAngle { get; set; } = 15f;
    public float OuterAngle { get; set; } = 25f;
}
