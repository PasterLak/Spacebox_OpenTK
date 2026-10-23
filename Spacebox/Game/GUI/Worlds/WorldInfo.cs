namespace Spacebox.Game.GUI;
using Engine;
using Engine.Utils;
using Spacebox.Game.Player.GameModes;
using System.IO;
using System.Text.Json;

public class WorldInfo
{
    public string Name { get; set; }
    public string Author { get; set; }
    public GameMode GameMode { get; set; } = GameMode.Creative;
    public string Seed { get; set; }
    public string ModId { get; set; }
    public List<string> Extensions { get; set; } = new List<string>();
    public string GameVersion { get; set; }
    public string LastEditDate { get; set; }
    public int Ticks { get; set; } = 0;
    public int Day { get; set; } = 0;
    public string FolderName { get; set; } = "";
    public bool ShowWelcomeWindow { get; set; } = true;
    public bool SpacerLooted { get; set; }
    public float[] SpawnPoint { get; set; }
    public int GeneratorVersion { get; set; } = Spacebox.Game.Generation.GeneratorVersions.Legacy;

    public bool IsLocalWorld = true;
    public Texture2D? WorldPreview;

    private PixelData? pixeldata;
    public static string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void UpdateEditDate()
    {
        LastEditDate = GetCurrentDate();
    }

    public static void LoadWorldPreview(string path, WorldInfo info)
    {
        if (info == null) return;
        if (!File.Exists(path)) return;


        var loadTask = PixelDataLoader.LoadAsync(path, false);
        try
        {
            Task.WaitAll(loadTask);
        }
        catch { }

        info.pixeldata = loadTask.IsCompletedSuccessfully ? loadTask.Result : null;

    }

    

    public static void CreatePreviewFromPixels(WorldInfo info)
    {
        if(info.pixeldata != null)
        {
            info.WorldPreview = new Texture2D(info.pixeldata, FilterMode.Nearest);
            Resources.AddResourceToDispose(info.WorldPreview);

            info.pixeldata = null;
        }
    }
}