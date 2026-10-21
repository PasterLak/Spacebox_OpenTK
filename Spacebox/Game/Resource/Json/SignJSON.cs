namespace Spacebox.Game.Resource;

public class SignJSON
{
    public int Width { get; set; } = 1;
    public int MaxLines { get; set; } = 4;
    public int MaxLineLength { get; set; } = 15;
    public string DefaultText { get; set; } = "";
    public string TextColor { get; set; } = "#2b2118";
    public string Align { get; set; } = "center";
    public int PixelsPerBlock { get; set; } = 96;
    public int Margin { get; set; } = 4;
    public bool Glow { get; set; } = false;
    public float ViewDistance { get; set; } = 48f;
    public string Font { get; set; } = "";
}
