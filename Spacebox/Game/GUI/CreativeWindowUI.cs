using Engine;
using Engine.Audio;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Spacebox.Game.GUI;

public class CreativeWindowCategory
{
    public Type Name;
    public List<Item> Items = new List<Item>();
    public Storage Storage;

    public int ItemsCount { get; private set; }

    public CreativeWindowCategory(Type name)
    {
        Name = name;

        if (name != null)
            Items = GetItems(name);

        if (Items.Count > 0)
        {
            Storage = GameAssets.CreateCreativeStorage(5, Items);
        }

        ItemsCount = Items.Count;
    }

    private static List<Item> GetItems(Type categoryType)
    {
        return GameAssets.Items.Values
            .Where(i =>
                (categoryType == typeof(CreativeToolItem) && i is CreativeToolItem) ||
                (categoryType != typeof(CreativeToolItem) && i.GetType() == categoryType))
            .Where(x => x.Id > 1)
            .OrderBy(i => i.Id)
            .ToList();
    }
}

public static class CreativeWindowUI
{
    private static float SlotSize = 64.0f;
    private static nint SlotTexture = nint.Zero;

    public static bool Enabled { get; set; } = false;

    private static LocalAstronaut player;
    private static Storage storageAll;
    private static CreativeWindowCategory[] categories;
    private static AudioSource scrollAudio;
    private static Storage selectedStorage;
    static int hovered = -1;

    public static void SetDefaultIcon(nint textureId, LocalAstronaut player)
    {
        SlotTexture = textureId;
        CreativeWindowUI.player = player;
        storageAll = GameAssets.CreateCreativeStorage(5, GameAssets.Items.Values.ToList());

        categories = GetCategories0()
         .Select(t => new CreativeWindowCategory(t))
         .OrderByDescending(c => c.Items.Count)
         .ToArray();

        selectedStorage = storageAll;

        scrollAudio = new AudioSource(Resources.Get<AudioClip>("scroll"));
    }

    private static void HandleHoverAudio(int slotId)
    {
        if (!ImGui.IsItemHovered()) return;
        if (hovered == slotId) return;

        hovered = slotId;
        if (scrollAudio.IsPlaying) scrollAudio.Stop();
        scrollAudio.Play();
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
        if (!Enabled || !UIManager.IsOpen("creative") || storageAll == null) return;

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
        ImGui.Begin("Creative", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y);
        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), "Creative");

        ImGui.SetCursorPos(new Vector2(padding, scrollY));

        ImGui.BeginChild("CreativeScroll2", new Vector2(scrollWidth, scrollHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDecoration);

        var selected = Theme.Colors.Background.ToUInt();
        var notselected = new Vector4(0.8f, 0.75f, 0.65f, 1.0f);

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
            }

            ImGui.PopStyleColor(1);

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();

                var total = 0;
                foreach (var cat in categories)
                {
                    total += cat.ItemsCount;
                }

                ImGui.Text("Show all items" + $" ({total})");
                ImGui.EndTooltip();
            }

            for (int i = 0; i < categories.Length; i++)
            {
                CreativeWindowCategory v = categories[i];
                ImGui.TableNextRow();
                for (int x = 0; x < 1; x++)
                {
                    ImGui.TableSetColumnIndex(x);

                    if (SlotTexture == nint.Zero)
                    {
                        if (ImGui.Button("ERR", new Vector2(SlotSize, SlotSize))) { }
                    }
                    else
                    {
                        if (selectedStorage == v.Storage)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, selected);
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, notselected);
                        }

                        if (selectedStorage == v.Storage)
                        {
                            selected = Theme.Colors.Background.ToUInt();
                            ImGui.PushStyleColor(ImGuiCol.Button, selected);
                            if (ImGui.Button("", new Vector2(SlotSize, SlotSize)))
                            {
                                selectedStorage = categories[i].Storage;
                            }
                        }
                        else
                        {
                            if (ImGui.ImageButton(v.Name.Name, SlotTexture, new Vector2(SlotSize, SlotSize)))
                            {
                                selectedStorage = categories[i].Storage;
                            }
                        }

                        var pos = ImGui.GetItemRectMin();
                        var size = new Vector2(SlotSize, SlotSize) * 0.8f;
                        var center = pos + new Vector2(SlotSize * 0.5f);
                        var dl = ImGui.GetWindowDrawList();

                        if (v.Items.Count > 0)
                            dl.AddImage(v.Items[0].IconTextureId, center - size * 0.5f, center + size * 0.5f);

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("Show " + v.Name.Name + $" ({v.ItemsCount})");
                            ImGui.EndTooltip();
                        }

                        ImGui.PopStyleColor(1);
                    }
                }
            }
            ImGui.EndTable();
        }

        ImGui.EndChild();

        ImGui.SetCursorPos(new Vector2(padding + padding + SlotSize, scrollY));
        ImGui.BeginChild("CreativeScroll", new Vector2(scrollWidth, scrollHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDecoration);

        ImGui.PushStyleColor(ImGuiCol.Button, Theme.Colors.Deep.ToUInt());
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

                    HandleHoverAudio(slot.SlotId);
                    InventoryUIHelper.ShowTooltip(slot, true, true, true);
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
                if (Input.IsAction("storage_item_quick_transfer"))
                {
                    if (player.Panel.TryAddItem(slot.Item, slot.Item.StackSize)) { }
                    else
                    {
                        player.Inventory.TryAddItem(slot.Item, slot.Item.StackSize);
                    }
                }
                else if (Input.IsKey(Keys.LeftControl))
                {
                    if (player.Panel.TryAddItem(slot.Item, (byte)(slot.Item.StackSize / 2))) { }
                    else
                    {
                        player.Inventory.TryAddItem(slot.Item, (byte)(slot.Item.StackSize / 2));
                    }
                }
                else
                {
                    if (player.Panel.TryAddItem(slot.Item, 1)) { }
                    else
                    {
                        player.Inventory.TryAddItem(slot.Item, 1);
                    }
                }
            }
        }
    }
}