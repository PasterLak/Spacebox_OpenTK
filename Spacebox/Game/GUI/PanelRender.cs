using System.Numerics;
using ImGuiNET;

using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI
{
  

    public static class PanelRenderer
    {
        public static void Render(Storage storage, float slotSize, nint slotTexture, nint selectedTexture, short selectedSlotId, float time)
        {
            if (!Settings.ShowInterface || !PanelUI.IsVisible || storage == null) return;

            ImGuiIOPtr io = ImGui.GetIO();
            slotSize = InventoryUIHelper.SlotSize;
            float windowWidth = storage.SizeY * slotSize;
            float windowHeight = storage.SizeX * slotSize;
            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 2f,
                (displaySize.Y - windowHeight) * 0.95f
            );

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight), ImGuiCond.Always);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            ImGui.Begin("Panel",
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoFocusOnAppearing
            );

            ImGui.SetCursorPos(Vector2.Zero);

            if (ImGui.BeginTable("PanelTable", storage.SizeY,
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.NoBordersInBody |
                ImGuiTableFlags.NoPadInnerX |
                ImGuiTableFlags.NoPadOuterX |
                ImGuiTableFlags.SizingFixedSame |
                ImGuiTableFlags.NoHostExtendX
            ))
            {
                for (int col = 0; col < storage.SizeY; col++)
                    ImGui.TableSetupColumn($"##col{col}", ImGuiTableColumnFlags.WidthFixed, slotSize);

                for (int row = 0; row < storage.SizeX; row++)
                {
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, slotSize);
                    for (int col = 0; col < storage.SizeY; col++)
                    {
                        ImGui.TableSetColumnIndex(col);
                        var slot = storage.GetSlot(row, col);
                        if (slot == null) continue;
                        string id = $"slot_{row}_{col}";
                        bool selected = (row == 0 && col == selectedSlotId);
                        InventoryUIHelper.DrawSlot(slot, id, PanelUI.OnSlotClicked, selected);
                        InventoryUIHelper.ShowTooltip(slot);
                    }
                }

                ImGui.EndTable();
            }

            ImGui.End();
            ImGui.PopStyleVar(3);
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
