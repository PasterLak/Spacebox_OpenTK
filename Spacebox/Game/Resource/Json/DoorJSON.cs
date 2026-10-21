namespace Spacebox.Game.Resource;

public class DoorJSON
{
    public string MoveDirection { get; set; } = "Right";
    public float Speed { get; set; } = 2f;
    public float OpenTime { get; set; } = 3f;
    public bool AutoClose { get; set; } = true;
    public string OpenSound { get; set; } = "";
    public string CloseSound { get; set; } = "";
}
