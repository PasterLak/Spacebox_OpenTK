
using Engine;

namespace Spacebox.Game.GUI
{
    public class VerticalLinkItem
    {
        public string Text { get; set; }
        public string Url { get; set; } // Empty or null if not a link
        public string IconPath { get; set; } // Empty or null if no icon

        public VerticalLinkItem(string text, string url = "", string iconPath = "")
        {
            Text = text;
            Url = url;
            IconPath = iconPath;
        }

        public bool HasLink => !string.IsNullOrEmpty(Url);
        public bool HasIcon => !string.IsNullOrEmpty(IconPath);
        
    }

    public static class VerticalLinks
    {
        public static bool IsVisible = false;

        private static List<VerticalLinkItem> linkItems = new List<VerticalLinkItem>();

        public static void Init()
        {

            linkItems = new List<VerticalLinkItem>
            {
                new VerticalLinkItem("Socials:"),
            };
            foreach (var link in Spacebox.Game.GUI.MenuUi.SocialLinks.All())
                linkItems.Add(new VerticalLinkItem("", link.Url, link.IconPath));

            foreach (var item in linkItems)
            {
                if (item.HasIcon)
                {
                    var texture = Engine.Resources.Get<Texture2D>(item.IconPath);
                    texture.FlipY();
                }
            }

            IsVisible = false;
        }
    }
}
