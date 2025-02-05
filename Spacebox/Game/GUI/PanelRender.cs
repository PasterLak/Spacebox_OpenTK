using System.Numerics;
using ImGuiNET;

using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI
{
  

    public static class PanelRenderer
    {
        public static void Render(Storage storage, float slotSize, nint slotTexture, nint selectedTexture, short selectedSlotId, float time)
        {
            if (!Settings.ShowInterface) return;
            if (!PanelUI.IsVisible) return;
            if (storage == null) return;
            ImGuiIOPtr io = ImGui.GetIO();
            slotSize = InventoryUIHelper.SlotSize;
            float windowWidth = storage.SizeY * slotSize;
            float windowHeight = storage.SizeX * slotSize;
            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2((displaySize.X - windowWidth) / 2, (displaySize.Y - windowHeight) * 0.95f);
            float padding = slotSize * 0.05f;
            Vector2 paddingV = new Vector2(padding, padding);
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight) + paddingV + paddingV);
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoTitleBar |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoScrollWithMouse |
                                           ImGuiWindowFlags.NoBringToFrontOnFocus |
                                           ImGuiWindowFlags.NoFocusOnAppearing;
            ImGui.Begin("Panel", windowFlags);
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight) + paddingV + paddingV, displaySize.Y, 0.003f);
            ImGui.SetCursorPos(paddingV);
            if (ImGui.BeginTable("PanelTable", storage.SizeY, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                for (int y = 0; y < storage.SizeY; y++)
                    ImGui.TableSetupColumn($"##columnPanel_{y}", ImGuiTableColumnFlags.WidthFixed, slotSize);
                for (int x = 0; x < storage.SizeX; x++)
                {
                    ImGui.TableNextRow();
                    for (int y = 0; y < storage.SizeY; y++)
                    {
                        ImGui.TableSetColumnIndex(y);
                        ItemSlot slot = storage.GetSlot(x, y);
                        if (slot == null) continue;
                        string id = $"slotPanel_{x}_{y}";
                        bool isSelected = (x == 0 && y == selectedSlotId);
                        InventoryUIHelper.DrawSlot(slot, id, PanelUI.OnSlotClicked, isSelected);
                        InventoryUIHelper.ShowTooltip(slot);
                    }
                }
                ImGui.EndTable();
            }
            ImGui.End();
        }

        public static void DrawItemName(string name)
        {
            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
            ImGui.Begin("ItemName", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);
            Vector2 textSize = ImGui.CalcTextSize(name);
            float posX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            float posY = (ImGui.GetWindowHeight() - textSize.Y) * 0.98f;
            ImGui.SetCursorPos(new Vector2(posX + 3, posY + 3));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));
            ImGui.TextUnformatted(name);
            ImGui.PopStyleColor();
            ImGui.SetCursorPos(new Vector2(posX, posY));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
            ImGui.TextUnformatted(name);
            ImGui.PopStyleColor();
            ImGui.End();
        }
    }
}
