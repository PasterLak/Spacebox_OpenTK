

namespace Spacebox.Game
{
    public class Settings
    {
        public static float FOV = 90;
        public static int ViewDistance = 10000;

        public const int LOD0 = 150;
        public const int LOD1 = 200;
        public const int LOD2 = 256;
        public const int LOD3 = 512;

        public const int CHUNK_VISIBLE_RADIUS = 900;
        public const int ENTITY_VISIBLE_RADIUS = 1000;
        public const int ENTITY_SEARCH_RADIUS = 1200;
        public const int VIEW_DISTANCE_TO_NEXT_SECTOR = ENTITY_SEARCH_RADIUS;
        public const int SECTOR_UNLOAD_DISTANCE = 1500;

        public static bool ShowInterface = true;


        public static void SaveSettings()
        {

        }

        public static void LoadSettings()
        {

        }
    }
}
