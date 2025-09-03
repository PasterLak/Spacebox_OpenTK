using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;

using Spacebox.Game.Player;
using Spacebox.Game.GUI.Menu;
using System.Drawing;

namespace Spacebox.Game.GUI
{

    public class CreativeWindowCategory
    {
        public Type Name;
        public List<Item> Items = new List<Item>();
        public Storage Storage;

        public CreativeWindowCategory(Type name)
        {
            Name = name;

            if (name != null)
                Items = GetItems(name);
          

            if (Items.Count > 0)
            {
                Storage = GameAssets.CreateCreativeStorage(5, Items);
            }
        }

        private static List<Item> GetItems(Type categoryType)
        {
            return GameAssets.Items.Values
                .Where(i =>
                    (categoryType == typeof(CreativeToolItem) && i is CreativeToolItem) ||
                    (categoryType != typeof(CreativeToolItem) && i.GetType() == categoryType))
                .Where(x => x.Id > 0)
                .OrderBy(i => i.Id)
                .ToList();
        }

        

    }
    public static class CreativeWindowUI
    {
        private static float SlotSize = 64.0f;
        private static nint SlotTexture = nint.Zero;

        public static bool IsVisible { get; set; } = false;
        public static bool Enabled { get; set; } = false;

        private static Astronaut player;

        private static Storage storageAll;

        private static CreativeWindowCategory[] categories;

      
        private static Storage selectedStorage;

        public static void SetDefaultIcon(nint textureId, Astronaut player)
        {
            SlotTexture = textureId;
            CreativeWindowUI.player = player;
            storageAll = GameAssets.CreateCreativeStorage(5, GameAssets.Items.Values.ToList());

            var inventory = ToggleManager.Register("creative");
            inventory.IsUI = true;
            inventory.OnStateChanged += s =>
            {
                IsVisible = s;
            };
            //Type[] cat = GetCategories0();

           // categories = new CreativeWindowCategory[cat.Length];

            categories = GetCategories0()                    
             .Select(t => new CreativeWindowCategory(t)) 
             .OrderByDescending(c => c.Items.Count)     
             .ToArray();

        

            selectedStorage = storageAll;
        }

        private static Type[] GetCategories0()
        {
            HashSet<Type> cats = new HashSet<Type>();
            foreach (var it in GameAssets.Items.Values)
            {
                Type t = it is CreativeToolItem ? typeof(CreativeToolItem) : it.GetType();
                cats.Add(t);
            }
            return cats.ToArray();
        }

        public static void OnGUI()
        {
            if (!Enabled || !IsVisible || storageAll == null) return;

            ImGuiIOPtr io = ImGui.GetIO();
            float titleBarHeight = ImGui.GetFontSize();
            SlotSize = InventoryUIHelper.SlotSize;
            float padding = SlotSize * 0.1f;
            float windowWidth = 6 * SlotSize + padding * 2;
            float windowHeight = 7 * SlotSize + titleBarHeight * 2 + padding;
            Vector2 displaySize = io.DisplaySize;
            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) / 8f,
                (displaySize.Y - windowHeight) / 2f
            );

            float scrollY = padding * 2 + titleBarHeight;
            float scrollHeight = windowHeight - scrollY - padding;
            float scrollWidth = windowWidth - padding;


            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight), ImGuiCond.Always);
            ImGui.Begin("Creative", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y);
            ImGui.SetCursorPos(new Vector2(padding, padding));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), "Creative");

            ImGui.SetCursorPos(new Vector2(padding, scrollY));

            ImGui.BeginChild("CreativeScroll2", new Vector2(scrollWidth, scrollHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDecoration);

           
              var selected = Theme.Colors.Background;


            var notselected = new Vector4(0.8f, 0.75f, 0.65f, 1.0f);
           // var notselected = Theme.Colors.Background;


            if (ImGui.BeginTable("CreativeTable2", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit, new Vector2(scrollWidth, scrollHeight)))
            {
                for (int x = 0; x < 1; x++)
                    ImGui.TableSetupColumn($"##columnCreative2_{x}", ImGuiTableColumnFlags.WidthFixed, SlotSize);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (selectedStorage == storageAll)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, selected);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, notselected);
                }
                if (ImGui.Button(" ALL\nTYPES", new Vector2(SlotSize, SlotSize)))
                {
                    selectedStorage = storageAll;
                };
                ImGui.PopStyleColor(1);  
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Show all items");
                    ImGui.EndTooltip();
                }

                ImGui.TableNextRow();
                for (int i = 0; i < categories.Length; i++)
                {
                    CreativeWindowCategory? v = categories[i];
                    ImGui.TableNextRow();
                    for (int x = 0; x < 1; x++)
                    {

                        ImGui.TableSetColumnIndex(x);

                        if (SlotTexture == nint.Zero)
                        {
                            if (ImGui.Button("ERR", new Vector2(SlotSize, SlotSize))) { };
                        }
                        else
                        {
                            if(selectedStorage == v.Storage)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Button, selected);
                            }
                            else
                            {
                                ImGui.PushStyleColor(ImGuiCol.Button, notselected);
                            }

                            // Debug.Log(v.Name.Name);
                            /* if (ImGui.Button(v.Items.Count.ToString(), new Vector2(SlotSize, SlotSize)))
                             {
                                 selectedStorage = categories[i].Storage;
                             };*/


                            if (selectedStorage == v.Storage)
                            {
                                selected = Theme.Colors.Background;
                                ImGui.PushStyleColor(ImGuiCol.Button, selected);
                                if (ImGui.Button("", new Vector2(SlotSize, SlotSize)))
                                {
                                  
                                    selectedStorage = categories[i].Storage;
                                }; 
                            }
                            else
                            {
                                if (ImGui.ImageButton(v.Name.Name, SlotTexture, new Vector2(SlotSize, SlotSize)))
                                {
                                    selectedStorage = categories[i].Storage;
                                };
                            }
                           

                            var pos = ImGui.GetItemRectMin();
                            var size = new Vector2(SlotSize, SlotSize) * 0.8f;
                            var center = pos + new Vector2(SlotSize * 0.5f);
                            var dl = ImGui.GetWindowDrawList();

                            if(v.Items.Count > 0) 
                            dl.AddImage(v.Items[0].IconTextureId, center - size * 0.5f, center + size * 0.5f);

                            // dl.AddImage(v.Items[0].IconTextureId, new Vector2(0, 0), new Vector2(SlotSize, SlotSize));

                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("Show " + v.Name.Name);
                                ImGui.EndTooltip();
                            }

                            ImGui.PopStyleColor(1);
                        }

                    }
                }
                ImGui.EndTable();
            }

            ImGui.EndChild();

            // --------------------------
            ImGui.SetCursorPos(new Vector2(padding + padding + SlotSize, scrollY));
            ImGui.BeginChild("CreativeScroll", new Vector2(scrollWidth, scrollHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDecoration);

            ImGui.PushStyleColor(ImGuiCol.Button, Theme.Colors.Deep);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 0));

            float scrollbarSize = ImGui.GetStyle().ScrollbarSize;
            float tableWidth = scrollWidth - scrollbarSize;

            if (ImGui.BeginTable("CreativeTable", selectedStorage.SizeX, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit, new Vector2(scrollWidth, scrollHeight)))
            {
                for (int x = 0; x < selectedStorage.SizeX; x++)
                    ImGui.TableSetupColumn($"##columnCreative_{x}", ImGuiTableColumnFlags.WidthFixed, SlotSize);

                for (int y = 0; y < selectedStorage.SizeY; y++)
                {
                    ImGui.TableNextRow();
                    for (int x = 0; x < selectedStorage.SizeX; x++)
                    {
                        ImGui.TableSetColumnIndex(x);
                        var slot = selectedStorage.GetSlot(x, y);
                        if (slot == null) continue;
                        string id = $"slotCreative_{x}_{y}";
                        if (SlotTexture == nint.Zero)
                        {
                            if (ImGui.Button("", new Vector2(SlotSize, SlotSize))) OnSlotClicked(slot);
                        }
                        else
                        {
                            if (ImGui.ImageButton(id, SlotTexture, new Vector2(SlotSize, SlotSize))) OnSlotClicked(slot);
                        }
                        if (slot.HasItem)
                        {
                            var pos = ImGui.GetItemRectMin();
                            var size = new Vector2(SlotSize, SlotSize) * 0.8f;
                            var center = pos + new Vector2(SlotSize * 0.5f);
                            var dl = ImGui.GetWindowDrawList();
                            dl.AddImage(GameAssets.ItemIcons[slot.Item.Id].Handle, center - size * 0.5f, center + size * 0.5f);
                            if (slot.Item.Is<CreativeToolItem>())
                            {
                                const string text = "c";
                                var tp = pos + new Vector2(SlotSize * 0.05f);
                                dl.AddText(tp + new Vector2(2, 2), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), text);
                                dl.AddText(tp, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.8f, 0, 1)), text);
                            }
                            if (slot.Count > 1)
                            {
                                var tp = pos + new Vector2(SlotSize * 0.05f);
                                dl.AddText(tp + new Vector2(2, 2), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), slot.Count.ToString());
                                dl.AddText(tp, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), slot.Count.ToString());
                            }
                        }
                        else
                        {
                            //if (ImGui.Button(" " + (slot.Item.Category), new Vector2(SlotSize, SlotSize))) OnSlotClicked(slot);
                        }
                        InventoryUIHelper.ShowTooltip(slot, true, true);
                    }
                }
                ImGui.EndTable();
            }

            ImGui.PopStyleColor(4);
            ImGui.EndChild();
            ImGui.End();
        }




        private static void OnSlotClicked(ItemSlot slot)
        {
            if (slot.HasItem)
            {
                if (player != null)
                {
                    if (Input.IsKey(Keys.LeftShift))
                    {
                        player.Panel.TryAddItem(slot.Item, slot.Item.StackSize);
                    }
                    else if (Input.IsKey(Keys.LeftControl))
                    {
                        player.Panel.TryAddItem(slot.Item, (byte)(slot.Item.StackSize / 2));
                    }
                    else
                    {
                        player.Panel.TryAddItem(slot.Item, 1);
                    }

                }
            }
            else
            {
            }
        }
    }
}
