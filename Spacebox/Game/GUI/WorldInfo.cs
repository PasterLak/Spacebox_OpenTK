using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;
using Engine;
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
    public bool ShowWelcomeWindow { get; set; } = true;

    public bool IsLocalWorld = true;



    public static string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void UpdateEditDate()
    {
        LastEditDate = GetCurrentDate();
    }
}