
using Spacebox.Core;

namespace LegacyWorldConverter
{
    public class LegacyConverter : IPlugin, IWorldConverter
    {
        public string TargetGameVersion => "0.1.3";

        public string Name => "LegacyConverter";

        public string Version => "1";

        public bool Convert(WorldInfo worldInfo)
        {
            bool changed = false;

            if (worldInfo.GameVersion == "0.0.8")
            {
                worldInfo.GameVersion = "0.0.9";
                changed = true;
            }
            if (worldInfo.GameVersion == "0.0.9")
            {
                worldInfo.GameVersion = "0.1.0";
                changed = true;
            }
            if (worldInfo.GameVersion == "0.1.0")
            {
                worldInfo.GameVersion = "0.1.1";
                changed = true;
            }
            if (worldInfo.GameVersion == "0.1.1")
            {
                worldInfo.GameVersion = "0.1.2";
                changed = true;
            }

            return changed;
        }

        public void OnInitialize(IGameApi api)
        {
            api.Debug.Success("[LegacyConverter] Initialized Legacy World Converter plugin.");
        }
    }
}
