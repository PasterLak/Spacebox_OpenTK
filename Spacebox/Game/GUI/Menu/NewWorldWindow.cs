namespace Spacebox.Game.GUI.Menu
{
    public class NewWorldWindow : MenuWindow
    {
        public GameMenu menu;

        public NewWorldWindow(GameMenu menu)
        {
            this.menu = menu;
        }
        protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.NewWorldPage(menu);
    }
}
