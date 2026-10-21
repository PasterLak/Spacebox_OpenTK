namespace Spacebox.Game.Resource;

public class PictureFrameJSON
{
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public List<string> Pictures { get; set; } = new List<string>();
    public float ViewDistance { get; set; } = 64f;
}
