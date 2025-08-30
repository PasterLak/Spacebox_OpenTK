using ImGuiNET;

using Engine.Audio;
using Engine;
using Spacebox.Game.Player;
using System.Numerics;
using Spacebox.Game.GUI.Menu;
using System.Diagnostics.Metrics;


namespace Spacebox.Game.GUI
{
    public class CraftingCategory
    {
        public string Id { get; set; } = "x";
        public string Name { get; set; } = "x";
        public string Icon { get; set; } = "";
        public IntPtr IconPtr { get; set; } = IntPtr.Zero;
        public int ItemsCountInCategory => Items.Count;
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
                if (_isVisible)
                {


                    openSound?.Play();

                }
                else
                {
                    closeSound?.Play();
                }
            }
        }

        private static bool showGrid = false;
        private static int selectedButton = -1;
        private static int usedSlots = 0;
        private static int totalSlots = 0;
        private static IntPtr SlotTexture;

        private static CraftingCategory[] category;
        private static Storage Inventory;
        private static Storage Panel;

        private static AudioSource scrollAudio;
        private static AudioSource clickAudio;

        private static AudioSource openSound;
        private static AudioSource closeSound;

        private static int ColumnsOfItems;


        public static void Toggle(Astronaut player)
        {
            var v = !IsVisible;
            ToggleManager.DisableAllWindows();

            if (v)
            {
                ToggleManager.SetState("crafting", true);
                ToggleManager.SetState("mouse", true);
                ToggleManager.SetState("player", false);
                if (player.GameMode != GameMode.Survival)
                    ToggleManager.SetState("creative", true);
                ToggleManager.SetState("inventory", true);
            }
            else
            {
                ToggleManager.SetState("mouse", false);
                ToggleManager.SetState("player", true);
            }

            Inventory = player.Inventory;
            Panel = player.Panel;
            showGrid = false;
        }
        static int CategoriesCountX = 0;
        const int CategoriesCountY = 2;
        public static void Init()
        {
            category = GameAssets.CraftingCategories.Values.ToArray();
            CategoriesCountX = (int)(category.Length/ CategoriesCountY) + (int)(category.Length % CategoriesCountY);

            var slotSize = CategoriesCountX * 1.5f;

            ColumnsOfItems = (int)Math.Ceiling(slotSize);

            var tex = Resources.Load<Texture2D>("Resources/Textures/slot.png");
            tex.FilterMode = FilterMode.Nearest;
            SlotTexture = tex.Handle;

            scrollAudio = new AudioSource(Resources.Load<AudioClip>("scroll"));
            clickAudio = new AudioSource(Resources.Load<AudioClip>("click1"));

            var inventory = ToggleManager.Register("crafting");
            inventory.IsUI = true;
            inventory.OnStateChanged += s =>
            {
                IsVisible = s;
            };

            if (openSound == null)
            {
                openSound = new AudioSource(Resources.Load<AudioClip>("openBlock1"));
                openSound.Volume = 1f;

            }

            if (closeSound == null)
            {
                closeSound = new AudioSource(Resources.Load<AudioClip>("openBlock4"));
                closeSound.Volume = 1f;

            }
        }
     
        public static void OnGUI()
        {
            if (!_isVisible) return;
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            //ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * (0.1f * CategoriesCountX);
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

               
                float buttonWidth = windowWidth / CategoriesCountX;
                float buttonHeight = (windowHeight - topOffset) / CategoriesCountY;
               
                for (int y = 0; y < CategoriesCountY; y++)
                {
                    for (int x = 0; x < CategoriesCountX; x++)
                    {
                        ImGui.SetCursorPos(new Vector2(
                            x * buttonWidth + spacing,
                            topOffset + y * buttonHeight + spacing
                        ));
                        float btnWidth = buttonWidth - spacing * 2;
                        float btnHeight = buttonHeight - spacing * 2;

                        var categoryIndex = y * CategoriesCountX + x;

                        if (categoryIndex >= category.Length) continue;
                     
                        string btnLabel = category[categoryIndex].Name;
                        CraftingCategoryButton(btnLabel, Vector2.Zero, btnWidth, btnHeight, (str) =>
                        {
                            selectedButton = categoryIndex;
                            usedSlots = category[selectedButton].ItemsCountInCategory;

                            int rows = (usedSlots + ColumnsOfItems - 1) / ColumnsOfItems;
                            totalSlots = 5 * ColumnsOfItems ;
                            _resetScroll = true;
                             showGrid = true;
                        }, y * CategoriesCountX + x);
                    }
                }
            }
            else
            {
                DrawGridWithSlots(windowWidth, windowHeight, topOffset / 2f);
            }

            // ImGui.PopStyleColor(6);
            ImGui.End();
        }
        static bool _resetScroll;
        private static void DrawGridWithSlots(float windowWidth, float windowHeight, float topOffset)
        {
            float spacing = windowHeight * 0.02f;
            float childX = spacing;
            float childY = spacing;
            float childW = windowWidth - spacing * 2;
            float childH = windowHeight - spacing * 2;

            string categoryName = category[selectedButton].Name;
            var titleSize = ImGui.CalcTextSize(categoryName);
            float backButtonWidth = topOffset * 2.5f;
            float backButtonHeight = topOffset;
            float headerHeight = spacing * 2 + backButtonHeight;

            ImGui.SetCursorPos(new Vector2(childX, childY));
            ImGui.BeginChild("HeaderChild", new Vector2(childW, headerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);
            ImGui.SetCursorPos(new Vector2((childW - titleSize.X) * 0.5f, spacing));
            ImGui.Text(categoryName);
            ImGui.SetCursorPos(new Vector2(childW - backButtonWidth - spacing, spacing));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1f));
            if (ImGui.Button("Back", new Vector2(backButtonWidth, backButtonHeight)))
            {
                clickAudio.Play();
                showGrid = false;
            }
            ImGui.PopStyleColor(3);
            ImGui.EndChild();

            ImGui.SetCursorPos(new Vector2(childX, childY + headerHeight));
            float gridHeight = childH - headerHeight - spacing;
            ImGui.BeginChild("GridScroll", new Vector2(childW, gridHeight), ImGuiChildFlags.None,  ImGuiWindowFlags.NoScrollbar);
            if (_resetScroll)
            {
                ImGui.SetScrollY(0f);
                _resetScroll = false;
            }
            ImGui.BeginTable("Grid", ColumnsOfItems, ImGuiTableFlags.NoBordersInBody, new Vector2(childW, gridHeight));
            for (int i = 0; i < totalSlots; i++)
            {
                ImGui.TableNextColumn();
                if (i < usedSlots)
                    SlotWithItem("", Vector2.Zero, childW / ColumnsOfItems, childW / ColumnsOfItems, OnClick, i);
                else
                    EmptySlot("", Vector2.Zero, childW / ColumnsOfItems, childW / ColumnsOfItems, OnClick, i);
            }
            ImGui.EndTable();
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
            if (Panel == null)
            {
                Debug.Error("[Craft] Panel is equal null!");
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

            if (TryGetResources(Inventory, Panel, blueprint))
            {
                if (Inventory.TryAddItem(blueprint.Product.Item, blueprint.Product.Quantity, out var rest))
                {

                }
                else
                {
                    Panel.TryAddItem(blueprint.Product.Item, rest);
                    // Debug.Log("Error try add item");
                }
            }
            else
            {

            }
        }

        private static bool TryGetResources(Storage s1, Storage s2, Blueprint blueprint)
        {
            foreach (var ing in blueprint.Ingredients)
            {
                int totalAvailable = s1.GetTotalCountOf(ing.Item) + s2.GetTotalCountOf(ing.Item);
                if (totalAvailable < ing.Quantity)
                    return false;
            }
            foreach (var ing in blueprint.Ingredients)
            {
                int required = ing.Quantity;
                int availableS1 = s1.GetTotalCountOf(ing.Item);
                if (availableS1 >= required)
                {
                    s1.RemoveItem(ing.Item, (byte)required);
                    continue;
                }
                else
                {
                    if (availableS1 > 0)
                        s1.RemoveItem(ing.Item, (byte)availableS1);
                    required -= availableS1;
                    int availableS2 = s2.GetTotalCountOf(ing.Item);
                    if (availableS2 >= required)
                        s2.RemoveItem(ing.Item, (byte)required);
                    else
                        s2.RemoveItem(ing.Item, (byte)availableS2);
                }
            }
            return true;
        }


        public static void EmptySlot(string label, Vector2 pos, float width, float height, Action<Blueprint> onClick, int slotId)
        {
            ImGui.Dummy(new Vector2(width, height));
            var p0 = ImGui.GetItemRectMin();
            var p1 = ImGui.GetItemRectMax();
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRect(p0, p1, Theme.Colors.BackgroundUint);
            drawList.AddImage(SlotTexture, p0, p1);

        }

        public static void SlotWithItem(string label, Vector2 pos, float width, float height, Action<Blueprint> onClick, int slotId)
        {
            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = height * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);
            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));
            var drawList = ImGui.GetWindowDrawList();

            ImGui.PushStyleColor(ImGuiCol.Button, Theme.Colors.Deep);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            //ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(1, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 0));

            //drawList.AddRectFilled(buttonPos - offset, buttonPos + new Vector2(width, height) + offset, borderColor);
            //drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height) + offset, lightColor);

            //drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height) , lightColor);
            //drawList.AddImage(SlotTexture, buttonPos, buttonPos + new Vector2(width, height) );


            var itemData = category[selectedButton].Items[slotId];

            label = itemData.item.Name;
            if (ImGui.Button($"##craft_slot_{slotId}", new Vector2(width, height)))
            {
                clickAudio.Play();
                onClick.Invoke(itemData.blueprint);
            };

            drawList.AddRect(buttonPos, buttonPos + new Vector2(width, height), Theme.Colors.BackgroundUint);
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
                    else if (Panel.HasItem(ing.Item, ing.Quantity))
                    {

                    }
                    else if (Inventory.GetTotalCountOf(ing.Item) + Panel.GetTotalCountOf(ing.Item) >= ing.Quantity)
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
                    var count = CalculatePossibleItemCraftCount(itemData.blueprint);
                    var t = count > 1 ? count.ToString() : "";
                    var text = ImGui.CalcTextSize(t);
                    drawList.AddImage(itemData.item.IconTextureId, buttonPos + offset, buttonPos + new Vector2(width, height) - offset);

                    drawList.AddText(buttonPos + new Vector2(offset.X + offset.X / 4f, height - text.Y), ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), t);
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

            ImGui.PopStyleColor(4);
            OnHovered(itemData, drawList, buttonPos, offset, width, height, slotId);
        }

        private static int CalculatePossibleItemCraftCount(Blueprint blueprint)
        {
            if (Inventory == null) return 0;
            if (Panel == null) return 0;
            if (blueprint == null) return 0;
            if (blueprint.Ingredients.Length == 0) return 0;

            int[] r = new int[blueprint.Ingredients.Length];

            var min = int.MaxValue;

            for (int i = 0; i < r.Length; i++)
            {
                r[i] = Inventory.GetTotalCountOf(blueprint.Ingredients[i].Item) + Panel.GetTotalCountOf(blueprint.Ingredients[i].Item);
                r[i] = r[i] / blueprint.Ingredients[i].Quantity;

                if (r[i] < min) min = r[i];
            }

            return min;
        }
        static int hovered = -1;
        private static void OnHovered(CraftingCategory.Data itemData, ImGuiNET.ImDrawListPtr list, Vector2 buttonPos, Vector2 offset, float width, float height, int slotId)
        {
            if (itemData?.item == null || Inventory == null || !ImGui.IsItemHovered()) return;

            HandleHoverAudio(slotId);
            DrawHoverImage(list, itemData, buttonPos, offset, width, height);
            ShowTooltip(itemData, height);
        }

        private static void HandleHoverAudio(int slotId)
        {
            if (hovered == slotId) return;

            hovered = slotId;
            if (scrollAudio.IsPlaying) scrollAudio.Stop();
            scrollAudio.Play();
        }

        private static void DrawHoverImage(ImGuiNET.ImDrawListPtr list, CraftingCategory.Data itemData, Vector2 buttonPos, Vector2 offset, float width, float height)
        {
            var startPos = buttonPos + offset;
            var endPos = buttonPos + new Vector2(width, height) - offset;
            list.AddImage(itemData.item.IconTextureId, startPos, endPos);
        }

        private static void ShowTooltip(CraftingCategory.Data itemData, float height)
        {
            ImGui.BeginTooltip();

            if (itemData.blueprint == null)
            {
                ShowNonCraftableItem(itemData);
            }
            else
            {
                ShowCraftableItem(itemData, height);
            }

            ImGui.EndTooltip();
        }

        private static void ShowNonCraftableItem(CraftingCategory.Data itemData)
        {
            ImGui.Text(itemData.item.Name ); ImGui.Spacing();
            var desc = itemData.item.Description;
            if(!string.IsNullOrWhiteSpace(desc))
            {
               InventoryUIHelper. DrawWrappedText(desc, 50, false);
            }
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Not craftable");
        }

        private static void ShowCraftableItem(CraftingCategory.Data itemData, float height)
        {
            if (itemData.blueprint.Product == null || itemData.blueprint.Ingredients == null)
            {
                ImGui.Text(itemData.item.Name); ImGui.Spacing();

                return;
            }

            ShowProductName(itemData);
            ShowIngredients(itemData.blueprint.Ingredients, height);
        }

        private static void ShowProductName(CraftingCategory.Data itemData)
        {
            var count = itemData.blueprint.Product.Quantity;
            var name = count > 1 ? $"{itemData.item.Name}({count})" : itemData.item.Name;
            ImGui.Text(name); ImGui.Spacing();
            var desc = itemData.item.Description;
         
            if (!string.IsNullOrWhiteSpace(desc))
            {
               
               InventoryUIHelper. DrawWrappedText(desc, 50, false);
              
            }
        }

        private static void ShowIngredients(Ingredient[] ingredients, float height)
        {
            foreach (var ingredient in ingredients)
            {
                var availableCount = Inventory.GetTotalCountOf(ingredient.Item) + Panel.GetTotalCountOf(ingredient.Item);
                var iconSize = new Vector2(1, 1) * (height / 2.8f);
                var textColor = availableCount >= ingredient.Quantity
                    ? new Vector4(0, 1, 0, 1)
                    : new Vector4(1, 0, 0, 1);

                ImGui.Image(ingredient.Item.IconTextureId, iconSize);
                ImGui.SameLine();
                ImGui.TextColored(textColor, ingredient.ToString());
            }
        }

        


        private static ImFontPtr LoadFont()
        {
            var io = ImGui.GetIO();

            return io.FontDefault;
        }


        public static void CraftingCategoryButton(string label, Vector2 pos, float width, float height, Action<string> onClick, int categoryId)
        {
            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = height * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);
            Vector2 padding = new Vector2(offsetValue, offsetValue) * 0.4f;


            uint lightColor = Theme.Colors.BackgroundUint;
            uint cc = ImGui.GetColorU32(new Vector4(1f, 1f, 0.0f, 1f));
            var drawList = ImGui.GetWindowDrawList();

            drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height), Theme.Colors.BorderLightUint);
            drawList.AddRectFilled(buttonPos + padding, buttonPos + new Vector2(width, height), Theme.Colors.BorderDarkUint);

            drawList.AddRectFilled(buttonPos + padding, buttonPos + new Vector2(width, height) - padding, Theme.Colors.BackgroundUint);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.6f, 0.6f, 0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 0.5f));

            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 0));

            if (ImGui.Button("##menuButton" + label, new Vector2(width, height)))
            {
                clickAudio.Play();
                onClick?.Invoke(label);

            }


            if (category[categoryId].IconPtr != IntPtr.Zero)
            {
                drawList.AddImage(category[categoryId].IconPtr, buttonPos + offset * 4, buttonPos + new Vector2(width, height) - offset);

            }
            drawList.AddText(LoadFont(), height / 6f, buttonPos + new Vector2(10, 10), Theme.Colors.Deep2Uint, "" + category[categoryId].Name);

            ImGui.PopStyleColor(4);
        }
    }
}
