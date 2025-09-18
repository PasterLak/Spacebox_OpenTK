

namespace Spacebox.Game
{
    public class Settings
    {
        public static GameSettings G { get; set; } = new GameSettings();
        public static AudioSettings Audio { get; set; } = new AudioSettings();
        public static GraphicsSettings Graphics { get; set; } = new GraphicsSettings();
        public static GameplaySettings Gameplay { get; set; } = new GameplaySettings();
        public static MetaSettings Meta { get; set; } = new MetaSettings();

        public static GameSettings AsGameSettings()
        {
            var newSettings = new GameSettings
            {
                Audio = Audio,
                Graphics = Graphics,
                Game = Gameplay,
                Meta = Meta
            };

            return newSettings;
        }

        public static int ViewDistance = 800;

        public const int LOD0 = 150;
        public const int LOD1 = 200;
        public const int LOD2 = 256;
        public const int LOD3 = 512;

        public const int CHUNK_VISIBLE_RADIUS = 900;
        public const int ENTITY_VISIBLE_RADIUS = 1000;
        public const int ENTITY_SEARCH_RADIUS = 1200;
        public const int VIEW_DISTANCE_TO_NEXT_SECTOR = ENTITY_SEARCH_RADIUS;
        public const int SECTOR_UNLOAD_DISTANCE = 1500;
        public const int SECTOR_UNLOAD_DISTANCE_SQUARED = SECTOR_UNLOAD_DISTANCE* SECTOR_UNLOAD_DISTANCE;

  

        public static bool ShowInterface = true;


        public static void SaveSettings()
        {

        }

        public static void LoadSettings()
        {

        }
    }
}
