namespace Spacebox.Game;

public sealed class RadarSettings
{
    public static RadarSettings Unlimited { get; } = new RadarSettings { Range = Settings.ENTITY_SEARCH_RADIUS, ShowMass = true };

    public float Range { get; set; } = 1000f;
    public bool ShowMass { get; set; }
}
