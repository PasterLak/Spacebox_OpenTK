

namespace Spacebox.Game.GUI.Menu
{
    public class OptionsWindow : MenuWindow
    {
        private readonly IOptionsNavigator _options;

        public OptionsWindow(IOptionsNavigator options)
        {
            _options = options;
            Settings.LoadSettings();
        }

        protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.OptionsListPage(_options);
    }
}
