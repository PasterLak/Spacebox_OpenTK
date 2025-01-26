
using System.Numerics;
using ImGuiNET;
using Spacebox.Common;

namespace Spacebox.Game.GUI
{
    public class VerticalLinkItem
    {
        public string Text { get; set; }
        public string Url { get; set; } // Empty or null if not a link
        public string IconPath { get; set; } // Empty or null if no icon
        public IntPtr IconHandle { get; set; } 

        public VerticalLinkItem(string text, string url = "", string iconPath = "")
        {
            Text = text;
            Url = url;
            IconPath = iconPath;
            IconHandle = IntPtr.Zero;
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
                new VerticalLinkItem("Join us:"), 
                new VerticalLinkItem("", "https://www.youtube.com/@SpaceboxGame", "Resources/Sprites/UI/youtube.png"),
                new VerticalLinkItem("", "https://discord.gg/9kY3dweXtY", "Resources/Sprites/UI/discord.png"),
                new VerticalLinkItem("", "https://vk.com/spacebox_official", "Resources/Sprites/UI/vk.png"),
                new VerticalLinkItem("", "https://pasterlak.itch.io/spacebox", "Resources/Sprites/UI/itchio.png"),

            };

            foreach (var item in linkItems)
            {
                if (item.HasIcon)
                {
                    var texture = TextureManager.GetTexture(item.IconPath);
                    texture.FlipY();
                    item.IconHandle = texture.Handle;
                }
            }

            IsVisible = false;
        }

        public static void Draw()
        {
            if (!IsVisible) return;

            Vector2 screenSize = ImGui.GetIO().DisplaySize;

            float buttonHeight = screenSize.Y * 0.05f;
            float buttonWidth = screenSize.X * 0.15f;
            float iconSize = buttonHeight * 0.8f;
            float padding = buttonHeight * 0.1f;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(buttonWidth, buttonHeight * linkItems.Count), ImGuiCond.Always);
            ImGui.Begin("VerticalMenu", ImGuiWindowFlags.NoTitleBar |
                                        ImGuiWindowFlags.NoResize |
                                        ImGuiWindowFlags.NoMove |
                                        ImGuiWindowFlags.NoScrollbar |
                                        ImGuiWindowFlags.NoScrollWithMouse |
                                        ImGuiWindowFlags.NoBackground);

            for (int i = 0; i < linkItems.Count; i++)
            {
                var item = linkItems[i];

                ImGui.SetCursorPos(new Vector2(0, i * buttonHeight));
                Vector2 btnPos = ImGui.GetCursorScreenPos();

                if (item.HasLink)
                {
                    ImGui.InvisibleButton($"btn{i}", new Vector2(buttonWidth, buttonHeight));

                    if (ImGui.IsItemClicked())
                    {
                        Application.OpenLink(item.Url);
                    }
                }
                else
                {
                    ImGui.Dummy(new Vector2(buttonWidth, buttonHeight));
                }

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                Vector2 iconPos = new Vector2(btnPos.X + padding, btnPos.Y + (buttonHeight - iconSize) / 2);
                Vector2 textPos = new Vector2(iconPos.X + (item.HasIcon ? iconSize + padding : 0), btnPos.Y + (buttonHeight - ImGui.GetTextLineHeight()) / 2);

                if (item.HasIcon && item.IconHandle != IntPtr.Zero)
                {
                    drawList.AddImage(item.IconHandle, iconPos, new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize));
                }


                uint textColor = item.HasLink
                    ? ImGui.GetColorU32(ImGuiCol.Text)
                    : ImGui.GetColorU32(ImGuiCol.TextDisabled);

                drawList.AddText(textPos, textColor, item.Text);
            }

            ImGui.End();
        }
    }
}
