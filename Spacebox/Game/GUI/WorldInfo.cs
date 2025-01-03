using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public class WorldInfo
{
    public string Name { get; set; }
    public string Author { get; set; }
    public GameMode GameMode { get; set; } = GameMode.Creative;
    public string Seed { get; set; }
    public string ModId { get; set; }
    public string GameVersion { get; set; }
    public string LastEditDate { get; set; }
    public string FolderName { get; set; } = "";

    public static string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void UpdateEditDate()
    {
        LastEditDate = GetCurrentDate();
    }
}