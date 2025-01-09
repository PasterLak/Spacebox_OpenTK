using ImGuiNET;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Game.Player;
using System.Numerics;


namespace Spacebox.Game.GUI
{
    public class CraftingCategory
    {
        public string Id { get; set; } = "x";
        public string Name { get; set; } = "x";
        public string Icon { get; set; } = "";
        public IntPtr IconPtr { get; set; } = IntPtr.Zero;
        public int SlotsCount => Items.Count;
        public List<Data> Items = new List<Data>();

        public class Data
        {
            public Item item;
            public Blueprint blueprint;
        }
    }
    public class CraftingGUI
    {
        protected static bool _isVisible = false;
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                ToggleManager.Instance.SetState("player", !_isVisible);
                ToggleManager.Instance.SetState("mouse", _isVisible);
                ToggleManager.Instance.SetState("inventory", _isVisible);
            }
        }

        private static bool showGrid = false;
        private static int selectedButton = -1;
        private static int usedSlots = 8;
        private static int totalSlots = 8;
        private static IntPtr SlotTexture;

        private static CraftingCategory[] category;
        private static Storage Inventory;

        private static AudioSource scrollAudio;
        public static void Toggle(Astronaut _)
        {
            IsVisible = !IsVisible;
            Inventory = _.Inventory;
            showGrid = false;
        }

        public static void Init()
        {
            category = GameBlocks.CraftingCategories.Values.ToArray();
            SlotTexture = TextureManager.GetTexture("Resources/Textures/slot.png", true, false).Handle;
            scrollAudio = new AudioSource(SoundManager.GetClip("scroll"));
        }

        public static void OnGUI()
        {
            if (!_isVisible) return;
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * 0.4f;
            float windowHeight = displaySize.Y * 0.25f;
            var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);
            ImGui.Begin("Crafting",
                ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoTitleBar
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                //| ImGuiWindowFlags.scr
                );
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            var textSize = ImGui.CalcTextSize("Crafting");
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.505f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.1f, 0.1f, 0.1f, 0.1f), "Crafting");
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 0.9f), "Crafting");

            float spacing = windowHeight * 0.02f;
            float topOffset = windowHeight * 0.2f;

            if (!showGrid)
            {

                const int countX = 4;
                const int countY = 2;
                float buttonWidth = windowWidth / countX;
                float buttonHeight = (windowHeight - topOffset) / countY;

                for (int y = 0; y < countY; y++)
                {
                    for (int x = 0; x < countX; x++)
                    {
                        ImGui.SetCursorPos(new Vector2(
                            x * buttonWidth + spacing,
                            topOffset + y * buttonHeight + spacing
                        ));
                        float btnWidth = buttonWidth - spacing * 2;
                        float btnHeight = buttonHeight - spacing * 2;
                        string btnLabel = category[y * countX + x].Name;
                        MenuButton(btnLabel, Vector2.Zero, btnWidth, btnHeight, (str) =>
                        {
                            selectedButton = y * countX + x;
                            usedSlots = category[selectedButton].SlotsCount;
                            //totalSlots = ((usedSlots + (6 - 1)) / 6) * 6;

                            totalSlots = 3 * 6;

                            showGrid = true;
                        }, y * countX + x);
                    }
                }
            }
            else
            {
                DrawGridWithSlots(windowWidth, windowHeight, topOffset / 2f);
            }

            ImGui.PopStyleColor(6);
            ImGui.End();
        }

        private static void DrawGridWithSlots(float windowWidth, float windowHeight, float topOffset)
        {
            float spacing = windowHeight * 0.02f;
            //float scrollbarSize = ImGui.GetStyle().ScrollbarSize;
            float childX = spacing;
            float childY = spacing;
            float childW = windowWidth - spacing * 2; // - scrollbarSize;
            float childH = windowHeight - spacing * 2;
            ImGui.SetCursorPos(new Vector2(childX, childY));
            ImGui.BeginChild("GridChild",
                new Vector2(childW, childH), // childW + scrollbarSize
                ImGuiChildFlags.None,
                ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollbar
                );

            string categoryName = $"{category[selectedButton].Name}";
            var titleSize = ImGui.CalcTextSize(categoryName);
            float titlePosX = (childW - titleSize.X) * 0.5f;
            ImGui.SetCursorPos(new Vector2(titlePosX, spacing));
            ImGui.Text(categoryName);

            float backButtonWidth = topOffset * 2.5f;
            float backButtonHeight = topOffset;
            float backButtonX = childW - backButtonWidth - spacing;
            float backButtonY = spacing;
            ImGui.SetCursorPos(new Vector2(backButtonX, backButtonY));

            if (ImGui.Button("Back", new Vector2(backButtonWidth, backButtonHeight)))
            {
                showGrid = false;
            }

            float gridOffsetY = spacing + backButtonHeight + spacing;
            ImGui.SetCursorPos(new Vector2(0, gridOffsetY));
            float gridHeight = childH - gridOffsetY - spacing;
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
            ImGui.BeginTable("Grid", 6, ImGuiTableFlags.NoBordersInBody, new Vector2(childW, gridHeight));
            for (int i = 0; i < totalSlots; i++)
            {
                ImGui.TableNextColumn();

                if (i < usedSlots)
                    SlotWithItem("", Vector2.Zero, childW / 6, childW / 6, OnClick, i);
                else
                    EmptySlot("", Vector2.Zero, childW / 6, childW / 6, OnClick, i);
            }

            ImGui.EndTable();
            ImGui.PopStyleVar();
            ImGui.EndChild();
        }

        private static void OnClick(Blueprint blueprint)
        {

            if (blueprint == null)
            {
                Debug.Error("[Craft] Blueprint is equal null!");
                return;
            }

            if (Inventory == null)
            {
                Debug.Error("[Craft] Inventory is equal null!");
                return;
            }
            if (blueprint.Product == null)
            {
                Debug.Error("[Craft] Blueprint.Product is equal null!");
                return;
            }
            if (blueprint.Product.Item == null)
            {
                Debug.Error("[Craft] Blueprint.Product.Item is equal null!");
                return;
            }

            if (TryGetResources(Inventory, blueprint))
            {

                if (Inventory.TryAddItem(blueprint.Product.Item, blueprint.Product.Quantity))
                {

                }
                else
                {
                    Debug.Log("Error try add item");
                }

            }
            else
            {

            }


        }

        private static bool TryGetResources(Storage storage, Blueprint blueprint)
        {
            //var item = blueprint.Ingredients[i].Item;
            //var quantity = blueprint.Products[0].Quantity;


            for (int i = 0; i < blueprint.Ingredients.Length; i++)
            {
                if (!storage.HasItem(blueprint.Ingredients[i].Item, blueprint.Ingredients[i].Quantity))
                    return false;
            }

            for (int i = 0; i < blueprint.Ingredients.Length; i++)
            {
                storage.DeleteItem(blueprint.Ingredients[i].Item, blueprint.Ingredients[i].Quantity);
            }

            return true;

        }

        public static void EmptySlot(string label, Vector2 pos, float width, float height, Action<Blueprint> onClick, int slotId)
        {
            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = height * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);
            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));
            var drawList = ImGui.GetWindowDrawList();

            drawList.AddRect(buttonPos, buttonPos + new Vector2(width, height), ImGui.GetColorU32(new Vector4(0.75f, 0.75f, 0.75f, 1f)));
            //drawList.AddImage(SlotTexture, buttonPos , buttonPos + new Vector2(width, height) );

            drawList.AddImage(SlotTexture, buttonPos, buttonPos + new Vector2(width, height)
                              );

        }

        public static void SlotWithItem(string label, Vector2 pos, float width, float height, Action<Blueprint> onClick, int slotId)
        {
            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = height * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);
            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));
            var drawList = ImGui.GetWindowDrawList();
            //drawList.AddRectFilled(buttonPos - offset, buttonPos + new Vector2(width, height) + offset, borderColor);
            //drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height) + offset, lightColor);

            //drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height) , lightColor);
            //drawList.AddImage(SlotTexture, buttonPos, buttonPos + new Vector2(width, height) );


            var itemData = category[selectedButton].Items[slotId];

            label = itemData.item.Name;
            if (ImGui.Button($"##craft_slot_{slotId}", new Vector2(width, height)))
            {
                onClick.Invoke(itemData.blueprint);
            };

            drawList.AddRect(buttonPos, buttonPos + new Vector2(width, height), ImGui.GetColorU32(new Vector4(0.75f, 0.75f, 0.75f, 1f)));
            //drawList.AddImage(SlotTexture, buttonPos , buttonPos + new Vector2(width, height) );

            drawList.AddImage(SlotTexture, buttonPos, buttonPos + new Vector2(width, height));


            var canCraft = true;

            if (itemData.blueprint == null) canCraft = false;

            if (canCraft)
            {
                foreach (var ing in itemData.blueprint.Ingredients)
                {

                    if (Inventory.HasItem(ing.Item, ing.Quantity))
                    {

                    }
                    else
                    {
                        canCraft = false;
                        break;
                    }
                }
            }


            if (itemData.item.IconTextureId != IntPtr.Zero)
            {
                if (canCraft)
                {
                    var count = CalculateItemCount(itemData.blueprint);
                    var t = count > 1 ? count.ToString() : "";
                    var text = ImGui.CalcTextSize(t);
                    drawList.AddImage(itemData.item.IconTextureId, buttonPos + offset, buttonPos + new Vector2(width, height) - offset);

                    drawList.AddText(buttonPos + new Vector2(offset.X + offset.X/4f, height - text.Y ), ImGui.GetColorU32(new Vector4(0,0,0, 1)), t);
                    drawList.AddText(buttonPos + new Vector2(offset.X, height - text.Y - text.Y / 12f), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1)), t);
                }
                else
                {
                    Vector4 tintColor2 = new Vector4(0.9f, 0.9f, 0.9f, 0.25f);


                    drawList.AddImage(itemData.item.IconTextureId, buttonPos + offset, buttonPos + new Vector2(width, height) - offset,
                                      Vector2.Zero, Vector2.One, ImGui.GetColorU32(tintColor2));


                }
                // drawList.AddRectFilled( buttonPos + offset, buttonPos + new Vector2(width, height) - offset, ImGui.GetColorU32(new Vector4(0f, 0.7f, 0f, 0.1f)));
                // else
                //    drawList.AddRectFilled(buttonPos + offset, buttonPos + new Vector2(width, height) - offset, ImGui.GetColorU32(new Vector4(0.7f, 0f, 0f, 0.1f)));


            }

            OnHovered(itemData, drawList, buttonPos, offset, width, height, slotId);
        }

        private static int CalculateItemCount(Blueprint blueprint)
        {
            if (Inventory == null) return 0;
            if (blueprint == null) return 0;
            if (blueprint.Ingredients.Length == 0) return 0;

            int[] r = new int[blueprint.Ingredients.Length];

            var min = int.MaxValue;

            for (int i = 0; i < r.Length; i++)
            {
                r[i] = Inventory.GetTotalCountOf(blueprint.Ingredients[i].Item);
                r[i] = r[i] / blueprint.Ingredients[i].Quantity;

                if (r[i] < min) min = r[i];
            }

            return min;
        }
        static int hovered = -1;
        private static void OnHovered(CraftingCategory.Data itemData, ImGuiNET.ImDrawListPtr list, Vector2 buttonPos, Vector2 offset, float width, float height, int slotId)
        {
            if (itemData == null) return;
            if (Inventory == null) return;

            if (ImGui.IsItemHovered())
            {
                if(hovered != slotId)
                {
                    hovered = slotId;
                    if(scrollAudio.IsPlaying) scrollAudio.Stop();
                    scrollAudio.Play();
                }
 
               
                list.AddImage(itemData.item.IconTextureId, buttonPos + offset, buttonPos + new Vector2(width, height) - offset);
                ImGui.BeginTooltip();

                if (itemData.blueprint == null)
                {
                    ImGui.Text(itemData.item.Name);
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Not craftable");
                }
                else
                {
                    if (itemData.blueprint.Product != null && itemData.blueprint.Ingredients != null)
                    {
                        var count = itemData.blueprint.Product.Quantity;
                        ImGui.Text(count > 1 ? $"{itemData.item.Name}({count})" : itemData.item.Name);
                        //ImGui.Text("Requared:");
                        foreach (var ing in itemData.blueprint.Ingredients)
                        {

                            if (Inventory.HasItem(ing.Item, ing.Quantity))
                            {
                                ImGui.TextColored(new Vector4(0, 1, 0, 1), ing.ToString());
                            }
                            else
                            {
                                ImGui.TextColored(new Vector4(1, 0, 0, 1), ing.ToString());
                            }
                        }
                    }
                    else ImGui.Text(itemData.item.Name);
                }


                // ImGui.TextColored(new Vector4(0,1,0,1), "x2 Iron Ingot" );
                //ImGui.TextColored(new Vector4(1, 0, 0, 1), "x3 Components");
                ImGui.EndTooltip();
            }
          
        }


        private static ImFontPtr LoadFont()
        {
            var io = ImGui.GetIO();

            return io.FontDefault;
        }


        public static void MenuButton(string label, Vector2 pos, float width, float height, Action<string> onClick, int categoryId)
        {
            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = height * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);
            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));
            uint cc = ImGui.GetColorU32(new Vector4(1f, 1f, 0.0f, 1f));
            var drawList = ImGui.GetWindowDrawList();
            //drawList.AddRectFilled(buttonPos - offset, buttonPos + new Vector2(width, height) + offset, borderColor);
            //drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height) + offset, lightColor);

            drawList.AddRectFilled(buttonPos - offset, buttonPos + new Vector2(width, height) + offset, lightColor);


            if (ImGui.Button("##menuButton" + label, new Vector2(width, height))) onClick?.Invoke(label);

            /* drawList.AddRectFilled(
                 buttonPos + new Vector2(0, height / 2f),
                 buttonPos + new Vector2(width/2f, height) + offset, cc);
            */

            if (category[categoryId].IconPtr != IntPtr.Zero)
            {
                drawList.AddImage(category[categoryId].IconPtr, buttonPos + offset * 4, buttonPos + new Vector2(width, height) - offset);

            }
            drawList.AddText(LoadFont(), height / 6f, buttonPos + new Vector2(5, 5), cc, category[categoryId].Name);


        }
    }
}
