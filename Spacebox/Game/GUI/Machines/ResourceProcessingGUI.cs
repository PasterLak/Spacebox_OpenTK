using Engine.Graphics.Atlas;
using Engine;
using Engine.Audio;
using ImGuiNET;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class ResourceProcessingGUI
    {
        static ResourceProcessingGUI()
        {
            InventoryUIHelper.ItemsDraggedOut += CreditProcessedItems;
        }

        protected static Recipe Recipe = new Recipe();
        protected static Storage InputStorage = new Storage(1, 1);
        protected static Storage FuelStorage = new Storage(1, 1);
        protected static Storage OutputStorage = new Storage(1, 1);
        protected static ResourceProcessingBlock processingBlock;

        public static string WindowName = "";
        private static AtlasSprite batteryIcon;
        private static AtlasSprite inputIcon;
        private static AtlasSprite outputIcon;
        protected static bool _wasVisible = false;

        private static string status = "";
        private static bool wasInitialized = false;

        private static AudioSource craftedSound;
        private static AudioSource openSound;
        private static AudioSource closeSound;
        private static AudioSource pickupSound;
        private static LocalAstronaut Player;

        public static void Toggle(LocalAstronaut player)
        {
            Player = player;
            if (!wasInitialized)
            {
                batteryIcon = AtlasSprite.None;
                inputIcon = AtlasSprite.None;
                outputIcon = AtlasSprite.None;
                wasInitialized = true;
            }

            if (player.GameMode != GameMode.Survival)
                UIManager.Toggle("resourceProcessing", "inventory", "creative");
            else
                UIManager.Toggle("resourceProcessing", "inventory");

            if (UIManager.IsOpen("resourceProcessing")) openSound?.Play();
            else closeSound?.Play();
        }

        private static void InitIcons()
        {
            batteryIcon = UIAtlas.Get("battery");

            if (WindowName.Contains("Crusher"))
            {
                inputIcon = UIAtlas.Get("crusherinput");
                outputIcon = UIAtlas.Get("crusheroutput");
            }
            else if (WindowName.Contains("Furnace"))
            {
                inputIcon = UIAtlas.Get("furnaceinput");
                outputIcon = UIAtlas.Get("furnaceoutput");
            }
        }

        public static void Activate(ResourceProcessingBlock block, LocalAstronaut player)
        {
            Player = player;
            if (!wasInitialized)
            {
                batteryIcon = AtlasSprite.None;
                inputIcon = AtlasSprite.None;
                outputIcon = AtlasSprite.None;
                wasInitialized = true;
            }

            var blockData = GameAssets.GetBlockDataById(block.Id);
            WindowName = blockData.Name;

            if (craftedSound == null || craftedSound.IsDisposed)
            {
                craftedSound = new AudioSource(Resources.Load<AudioClip>("crafted"), AudioChannel.UI);
                craftedSound.Volume = 1f;
            }
            if (openSound == null || openSound.IsDisposed)
            {
                openSound = new AudioSource(Resources.Load<AudioClip>("openBlock1"), AudioChannel.UI);
                openSound.Volume = 1f;
            }
            if (closeSound == null || closeSound.IsDisposed)
            {
                closeSound = new AudioSource(Resources.Load<AudioClip>("openBlock4"), AudioChannel.UI);
                closeSound.Volume = 1f;
            }
            if (pickupSound == null || pickupSound.IsDisposed)
            {
                pickupSound = new AudioSource(Resources.Load<AudioClip>("pickupDefault"), AudioChannel.UI);
                pickupSound.Volume = 0.5f;
            }

            InitIcons();
            if (processingBlock != null) processingBlock.OnCrafted -= OnCrafted;
            processingBlock = block;

            if (InputStorage != null) InputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            if (OutputStorage != null) OutputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            if (FuelStorage != null) FuelStorage.OnDataWasChanged -= OnInputItemWasChanged;

            status = "Status: Ready";
            InputStorage.DisconnectStorage();
            FuelStorage.DisconnectStorage();
            OutputStorage.DisconnectStorage();

            InputStorage = processingBlock.InputStorage;
            FuelStorage = processingBlock.FuelStorage;
            OutputStorage = processingBlock.OutputStorage;

            block.OnCrafted -= OnCrafted;
            block.OnCrafted += OnCrafted;

            InputStorage.ConnectStorage(player.Inventory, true);
            FuelStorage.ConnectStorage(player.Inventory, false);
            OutputStorage.ConnectStorage(player.Inventory, false);

            player.Inventory.ConnectStorage(InputStorage, false);
            player.Panel.ConnectStorage(InputStorage, false);

            InputStorage.OnDataWasChanged += OnInputItemWasChanged;
            OutputStorage.OnDataWasChanged += OnInputItemWasChanged;
            FuelStorage.OnDataWasChanged += OnInputItemWasChanged;

            if (block.TryStartTask(out var task))
            {
                TickTaskManager.AddTask(task);
            }
            UpdateStatus();

            if (player.GameMode != GameMode.Survival)
                UIManager.Open("resourceProcessing", "inventory", "creative");
            else
                UIManager.Open("resourceProcessing", "inventory");
        }

        private static void OnCrafted(ResourceProcessingBlock block)
        {
            if (!UIManager.IsOpen("resourceProcessing")) return;

            if (craftedSound.IsPlaying)
            {
                craftedSound.Stop();
                craftedSound.Play();
            }
            else
            {
                craftedSound.Play();
            }
        }

        private static void OnInputItemWasChanged(Storage storage)
        {
            if (processingBlock != null)
            {
                if (!processingBlock.IsRunning && processingBlock.TryStartTask(out var task))
                {
                    TickTaskManager.AddTask(task);
                }
                UpdateStatus();
            }
        }

        private static void UpdateStatus()
        {
            if (processingBlock == null)
            {
                status = "Status: Done";
                return;
            }

            var inputSlot = InputStorage.GetSlot(0, 0);

            if (inputSlot.Count == 0)
            {
                status = "Status: Done";
                processingBlock.StopTask();
            }
            else if (processingBlock.IsRunning)
            {
                status = "Status: " + processingBlock.GetTaskProgress() + "/100";
            }
            else
            {
                bool isValidIngredient = false;
                bool hasEnoughQuantity = false;

                if (inputSlot.HasItem && GameAssets.Recipes.ContainsKey(processingBlock.BlockType))
                {
                    foreach (var recipe in GameAssets.Recipes[processingBlock.BlockType].Values)
                    {
                        if (recipe.Ingredient.Item == inputSlot.Item)
                        {
                            isValidIngredient = true;
                            if (inputSlot.Count >= recipe.Ingredient.Quantity)
                            {
                                hasEnoughQuantity = true;
                            }
                            break;
                        }
                    }
                }

                if (isValidIngredient)
                {
                    if (hasEnoughQuantity)
                    {
                        status = "Status: Output blocked!";
                    }
                    else
                    {
                        status = "Status: Not enough resource!";
                    }
                }
                else
                {
                    status = "Status: Wrong ingredient!";
                }

                processingBlock.StopTask();
            }
        }

        public static void Reset()
        {
            if (processingBlock != null) processingBlock.OnCrafted -= OnCrafted;
            InputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            OutputStorage.OnDataWasChanged -= OnInputItemWasChanged;
            FuelStorage.OnDataWasChanged -= OnInputItemWasChanged;

            InputStorage = new Storage(1, 1);
            FuelStorage = new Storage(1, 1);
            OutputStorage = new Storage(1, 1);
            processingBlock = null;
            Player = null;
            _wasVisible = false;
        }

        public static void OnGUI()
        {
          

            bool isVisible = UIManager.IsOpen("resourceProcessing");

            if (isVisible != _wasVisible)
            {
                if (isVisible)
                {
                    openSound?.Play();
                }
                else
                {
                    closeSound?.Play();
                    if (Player != null)
                    {
                        Player.Panel.ConnectStorage(Player.Inventory, true);
                        Player.Inventory.ConnectStorage(Player.Panel);
                    }
                    if (processingBlock != null)
                    {
                        processingBlock.OnCrafted -= OnCrafted;
                    }
                }
                _wasVisible = isVisible;
            }

            if (!isVisible) return;

            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * 0.5f;
            float windowHeight = displaySize.Y * 0.25f;
            var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);

            ImGui.Begin(WindowName, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

            if (processingBlock != null)
            {
                if (processingBlock.IsRunning)
                {
                    status = "Status: " + processingBlock.GetTaskProgress() + "/100";
                }
            }

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            float leftPanelWidth = windowWidth * 0.68f;
            float rightPanelWidth = windowWidth * 0.3f;
            float spacing = windowHeight * 0.03f;
            var slotSize = InventoryUIHelper.SlotSize;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0));
            ImGui.BeginChild("LeftPanel", new Vector2(leftPanelWidth, windowHeight), ImGuiChildFlags.None);

            var textSize = ImGui.CalcTextSize(WindowName);
            ImGui.SetCursorPos(new Vector2(leftPanelWidth * 0.505f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.1f, 0.1f, 0.1f, 0.1f), WindowName);
            ImGui.SetCursorPos(new Vector2(leftPanelWidth * 0.5f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 0.9f), WindowName);

            var textSize2 = ImGui.CalcTextSize(status);
            ImGui.SetCursorPos(new Vector2(leftPanelWidth * 0.5f - textSize2.X * 0.5f, textSize.Y * 2.2f));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 0.9f), status);

            float centerX = leftPanelWidth * 0.5f;
            float centerY = windowHeight * 0.5f;
            float slotSpacing = slotSize * 1.5f;

            ImGui.SetCursorPos(new Vector2(centerX - slotSpacing, centerY - slotSize * 0.7f));
            InventoryUIHelper.DrawSlot(InputStorage.GetSlot(0, 0), "InputStorage", MoveItems, inputIcon, false);
            InventoryUIHelper.ShowTooltip(InputStorage.GetSlot(0, 0), false, true);

            //ImGui.SetCursorPos(new Vector2(centerX - slotSpacing, centerY + slotSpacing * 0.5f));
            //InventoryUIHelper.DrawSlot(FuelStorage.GetSlot(0, 0), "FuelStorage", MoveItems, batteryIcon, false);
           // InventoryUIHelper.ShowTooltip(FuelStorage.GetSlot(0, 0), false, true);

            ImGui.SetCursorPos(new Vector2(centerX + slotSpacing * 0.5f, centerY - slotSize * 0.7f));
            InventoryUIHelper.DrawSlot(OutputStorage.GetSlot(0, 0), "OutputStorage", MoveItems, outputIcon, false);
            InventoryUIHelper.ShowTooltip(OutputStorage.GetSlot(0, 0), false, true);

            ImGui.EndChild();
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(ImGui.GetCursorScreenPos().X, windowPos.Y + spacing),
                new Vector2(ImGui.GetCursorScreenPos().X, windowPos.Y + windowHeight - spacing),
                ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0.7f, 0.9f)));
            ImGui.SameLine();

            float scrollBarWidth = spacing;
            float listContentWidth = rightPanelWidth - spacing - scrollBarWidth;

            var size = new Vector2(rightPanelWidth - spacing * 2, windowHeight - spacing * 5);
            ImGui.SetCursorPos(new Vector2(windowWidth, windowHeight - spacing * 4) - size);
            ImGui.Text("  Recipes");
            ImGui.SetCursorPos(new Vector2(windowWidth - spacing, windowHeight - spacing) - size);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, Theme.Colors.Deep.ToUInt());
            ImGui.BeginChild("RecipeList", size, ImGuiChildFlags.None);

            DrawRecipeList(listContentWidth, displaySize.Y);

            ImGui.EndChild();

            ImGui.PopStyleColor(6);
            ImGui.End();
        }

        private static void DrawRecipeList(float listWidth, float displayHeight)
        {
            if (processingBlock == null) { return; }

            float scaleFactor = displayHeight / 1080f;
            float fixedItemHeight = 45f * scaleFactor;
            float imageSize = 40f * scaleFactor;
            float numberSize = ImGui.GetTextLineHeight();

            if (!GameAssets.Recipes.TryGetValue(processingBlock.BlockType, out var recipesById)) return;
            List<Recipe> recipes = recipesById.Values.ToList();

            for (int i = 0; i < recipes.Count; i++)
            {
                float startY = ImGui.GetCursorPosY();
                var recipe = recipes[i];

                if (recipe == null || recipe.Ingredient.Item == null || !recipe.Ingredient.Item.Icon.IsValid)
                {
                    continue;
                }

                var inQ = recipe.Ingredient.Quantity > 1 ? recipe.Ingredient.Quantity.ToString() : "";
                var inItem = recipe.Ingredient.Item;
                var inImg = inItem.Icon;

                var prQ = recipe.Product.Quantity > 1 ? recipe.Product.Quantity.ToString() : "";
                var prItem = recipe.Product.Item;
                var prImg = prItem.Icon;

                var imageVector = new Vector2(imageSize, imageSize);
                var offsetX = new Vector2(imageSize * 0.3f, startY);

                ImGui.SetCursorPos(offsetX);
                inImg.Image(imageVector);
                UIHelper.ShowTooltip(inItem.Name);

                var pos = new Vector2(imageSize - ImGui.CalcTextSize(inQ).X - 2 + offsetX.X, startY + imageSize - numberSize);
                pos = pos + imageVector * 0.1f;
                ImGui.SetCursorPos(pos);
                ImGui.TextColored(new Vector4(0f, 0f, 0f, 1), inQ);
                ImGui.SetCursorPos(pos + imageVector * 0.04f);
                ImGui.TextColored(new Vector4(1), inQ);

                var arrowSize = ImGui.CalcTextSize("->");

                ImGui.SetCursorPos(new Vector2(listWidth * 0.5f - ImGui.CalcTextSize("->").X * 0.4f, startY + imageSize * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                ImGui.Text("->");

                ImGui.SetCursorPos(new Vector2(listWidth - imageSize * 1.4f, startY));

                prImg.Image(imageVector);
                UIHelper.ShowTooltip(prItem.Name + "\n" + "Time: " + (processingBlock.GetTicksWithEfficiency(recipe.RequiredTicks) / 20f).ToString("#.#") + "s.");

                pos = new Vector2(listWidth * 0.94f - ImGui.CalcTextSize(prQ).X - 2, startY + imageSize - numberSize);

                ImGui.SetCursorPos(pos);
                ImGui.TextColored(new Vector4(0f, 0f, 0f, 1), prQ);
                ImGui.SetCursorPos(pos + imageVector * 0.05f);
                ImGui.TextColored(new Vector4(1), prQ);

                ImGui.SetCursorPosY(startY + fixedItemHeight);
            }
        }

        private static void MoveItems(ItemSlot slot)
        {
            if (!slot.HasItem) return;
            if (!Input.IsAction("storage_item_quick_transfer")) return;

            if (slot.Storage.ConnectedStorage == null)
            {
                Debug.Error("No connected storage");
                return;
            }

            var itemBefore = slot.Item;
            int countBefore = slot.Count;

            if (slot.TryMoveItemToConnectedStorage(out var rest))
            {
                RestartPickupSound();
            }
            else
            {
                TryMoveToPlayerPanel(slot, rest);
            }

            CreditProcessedItems(slot, itemBefore, PlayerStatistics.CountMovedFrom(itemBefore, countBefore, slot.Item, slot.Count));
        }

        private static void CreditProcessedItems(ItemSlot slot, Item item, int moved)
        {
            if (moved <= 0 || slot.Storage != OutputStorage || Player == null) return;

            Player.PlayerStatistics.AddProcessedItems(moved);
            if (item != null) PlayerActivity.Raise(PlayerActivityKind.ItemProcessed, item.Id, moved, processingBlock?.BlockType);
        }

        private static void TryMoveToPlayerPanel(ItemSlot slot, byte rest)
        {
            if (Player?.Panel == null) return;

            Player.Panel.TryAddItem(slot.Item, rest, out var notAdded);
            if (notAdded < rest)
            {
                slot.Count = (byte)(slot.Count - (rest - notAdded));
            }
        }

        private static void RestartPickupSound()
        {
            if (pickupSound.IsPlaying)
            {
                pickupSound.Stop();
            }
            pickupSound.Play();
        }
    }
}