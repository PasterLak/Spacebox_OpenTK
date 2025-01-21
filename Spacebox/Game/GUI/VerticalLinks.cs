using System;
using System.Numerics;
using ImGuiNET;
using Spacebox.Common;

namespace Spacebox.Game.GUI
{


    public static class VerticalLinks
    {

        public static bool IsVisible = false;

        private static readonly string[] urls = new string[]
        {
            "",

        "https://www.youtube.com/@SpaceboxGame",
        "https://discord.gg/9kY3dweXtY",
        "https://vk.com/spacebox_official",
        "https://pasterlak.itch.io/spacebox"
        };

        private static readonly string[] texts = new string[]
        {
            "Join us",
        "",
        "",
        "",
        ""
        };

        private static IntPtr[] icons = new IntPtr[]
        {
            IntPtr.Zero,
        new IntPtr(1),
        new IntPtr(2),
        new IntPtr(3),
        new IntPtr(4)
        };

        public static void Init()
        {
            var tex0 = TextureManager.GetTexture("Resources/Sprites/UI/youtube.png");
            var tex1 = TextureManager.GetTexture("Resources/Sprites/UI/discord.png");
            var tex2 = TextureManager.GetTexture("Resources/Sprites/UI/vk.png");
            var tex3 = TextureManager.GetTexture("Resources/Sprites/UI/itchio.png");

            tex0.FlipY();
            tex1.FlipY();
            tex2.FlipY();
            tex3.FlipY();


            icons[1] = tex0.Handle;
            icons[2] = tex1.Handle;
            icons[3] = tex2.Handle;
            icons[4] = tex3.Handle;

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
            ImGui.SetNextWindowSize(new Vector2(buttonWidth, buttonHeight * 5), ImGuiCond.Always);
            ImGui.Begin("VerticalMenu", ImGuiWindowFlags.NoTitleBar |
                                            ImGuiWindowFlags.NoResize |
                                            ImGuiWindowFlags.NoMove |
                                            ImGuiWindowFlags.NoScrollbar |
                                            ImGuiWindowFlags.NoScrollWithMouse |
                                            ImGuiWindowFlags.NoBackground);


            for (byte i = 0; i < 5; i++)
            {
                ImGui.SetCursorPos(new Vector2(0, i * buttonHeight));
                Vector2 btnPos = ImGui.GetCursorScreenPos();
                ImGui.InvisibleButton("btn" + i, new Vector2(buttonWidth, buttonHeight));

                if (ImGui.IsItemClicked())
                {
                    if (urls[i] != "")
                        Application.OpenLink(urls[i]);
                }

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                Vector2 iconPos = new Vector2(btnPos.X + padding, btnPos.Y + (buttonHeight - iconSize) / 2);
                Vector2 textPos = new Vector2(iconPos.X + iconSize + padding, btnPos.Y + (buttonHeight - ImGui.GetTextLineHeight()) / 2);
                if (icons[i] != IntPtr.Zero)
                    drawList.AddImage(icons[i], iconPos, new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize));
                drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), texts[i]);
                //ImGui.Dummy(new Vector2(0, i * buttonHeight));
            }

            ImGui.End();
        }
    }

}
