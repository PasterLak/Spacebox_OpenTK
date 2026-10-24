namespace Spacebox.Game.GUI.Menu
{
    public class WorldSelectWindow : MenuWindow
    {
        public GameMenu menu;
        public WorldSelectWindow(GameMenu menu)
        {
            this.menu = menu;
        }

        protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.WorldSelectPage(menu);
    }
}
