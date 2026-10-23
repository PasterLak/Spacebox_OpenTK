
using Engine;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.GUI
{
    public class UpdateVersionWindow
    {
        private GameMenu menu;
        private string _error;
        private WorldUpgradeResult _result;
        private WorldInfo _checkedWorld;
        private WorldUpgradeStatus _status;
        private string _reason;

        public UpdateVersionWindow(GameMenu menu)
        {
            this.menu = menu;
        }

        public void Reset()
        {
            _error = null;
            _result = null;
            _checkedWorld = null;
            _reason = null;
        }

        public GameMenu Menu => menu;
        public string Error => _error;
        public WorldUpgradeResult Result => _result;
        public WorldUpgradeStatus Status => _status;
        public string Reason => _reason;

        public void EnsureChecked(WorldInfo world)
        {
            if (_checkedWorld == world) return;

            _checkedWorld = world;
            _status = VersionConverter.CheckWorld(world, out _reason);
        }

        public void Convert(WorldInfo world)
        {
            menu.Click1.Play();
            if (!VersionConverter.Convert(world, Application.Version))
            {
                _error = "Conversion failed: " + (VersionConverter.LastResult?.Error ?? "unknown error");
                return;
            }

            world.LastEditDate = DateTime.Now.ToString();
            WorldInfoSaver.Save(world);
            _error = null;

            var result = VersionConverter.LastResult;
            if (result != null && result.Warnings.Count > 0)
            {
                _result = result;
                return;
            }

            Close();
        }

        public void Close()
        {
            Reset();
            menu.showVersionConvertWindow = false;
        }
    }
}
