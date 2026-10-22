

namespace Spacebox.Game;

public class Settings
{
 
    public static AudioSettings Audio { get; set; } = new AudioSettings();
    public static GraphicsSettings Graphics { get; set; } = new GraphicsSettings();
    public static InterfaceSettings Interface { get; set; } = new InterfaceSettings();
    public static GameplaySettings Gameplay { get; set; } = new GameplaySettings();
    public static MetaSettings Meta { get; set; } = new MetaSettings();
    public static TexturePackSettings TexturePacks { get; set; } = new TexturePackSettings();

    public static GameSettings AsGameSettings()
    {
        var newSettings = new GameSettings
        {
            Audio = Audio,
            Graphics = Graphics,
            Interface = Interface,
            Game = Gameplay,
            Meta = Meta,
            TexturePacks = TexturePacks
        };

        return newSettings;
    }

    public static int ViewDistance = 2200; // change?

    public const int LOD0 = 150;
    public const int LOD1 = 256;
    public const int LOD2 = 512;
    public const int LOD3 = MAX_CHUNK_VISIBLE_RADIUS; // = CHUNK_VISIBLE_RADIUS ?  512 old

    public const int MAX_CHUNK_VISIBLE_RADIUS = 900;
    public static int CHUNK_VISIBLE_RADIUS => Gameplay.DrawDistance * MAX_CHUNK_VISIBLE_RADIUS / GameplaySettings.MaxDrawDistance;
    public static int ENTITY_VISIBLE_RADIUS => Gameplay.DrawDistance;
    public const int ENTITY_SEARCH_RADIUS = 1200;
    public const int ENTITY_UNLOAD_DISTANCE = 1500;
    public const int VIEW_DISTANCE_TO_NEXT_SECTOR = ENTITY_SEARCH_RADIUS;
    public const int SECTOR_UNLOAD_DISTANCE = 2000;
    public const int SECTOR_UNLOAD_DISTANCE_SQUARED = SECTOR_UNLOAD_DISTANCE* SECTOR_UNLOAD_DISTANCE;


    public static bool ShowInterface = true;


    public static void SaveSettings()
    {

    }

    public static void LoadSettings()
    {
        var settings = SettingsService.Load();

        Audio = settings.Audio;
        Gameplay = settings.Game;
        Graphics = settings.Graphics;
        Interface = settings.Interface;
        Meta = settings.Meta;
        TexturePacks = settings.TexturePacks;
    }
}
