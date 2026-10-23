using Engine.Graphics.Atlas;
using Engine;
using Engine.Audio;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spacebox.Game.GUI
{
    public class ResourceProcessingGUI
    {
        static ResourceProcessingGUI()
        {
            SlotTransfer.ItemsDraggedOut += CreditProcessedItems;
        }

        protected static Recipe Recipe = new Recipe();
        protected static Storage InputStorage = new Storage(1, 1);
        protected static Storage FuelStorage = new Storage(1, 1);
        protected static Storage OutputStorage = new Storage(1, 1);
        protected static ProcessingComponent processingBlock;
        protected static Storage BufferStorage;

        public static string WindowName = "";
        private static AtlasSprite batteryIcon;
        private static AtlasSprite inputIcon;
        private static AtlasSprite outputIcon;
        protected static bool _wasVisible = false;

        private static string status = "";
        private static bool wasInitialized = false;

        private static AudioSource craftedSound;
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

        public static void Activate(ProcessingComponent block, LocalAstronaut player)
        {
            Player = player;
            if (!wasInitialized)
            {
                batteryIcon = AtlasSprite.None;
                inputIcon = AtlasSprite.None;
                outputIcon = AtlasSprite.None;
                wasInitialized = true;
            }

            var blockData = GameAssets.GetBlockDataById(block.Host.Id);
            WindowName = blockData.Name;

            if (craftedSound == null || craftedSound.IsDisposed)
            {
                craftedSound = new AudioSource(Resources.Load<AudioClip>("crafted"), AudioChannel.UI);
                craftedSound.Volume = 1f;
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
            if (BufferStorage != null) BufferStorage.OnDataWasChanged -= OnInputItemWasChanged;

            status = "Status: Ready";
            InputStorage.DisconnectStorage();
            FuelStorage.DisconnectStorage();
            OutputStorage.DisconnectStorage();
            BufferStorage?.DisconnectStorage();

            InputStorage = processingBlock.InputStorage;
            FuelStorage = processingBlock.FuelStorage;
            OutputStorage = processingBlock.OutputStorage;
            BufferStorage = processingBlock.BufferStorage;

            block.OnCrafted -= OnCrafted;
            block.OnCrafted += OnCrafted;

            InputStorage.ConnectStorage(player.Inventory, true);
            FuelStorage.ConnectStorage(player.Inventory, false);
            OutputStorage.ConnectStorage(player.Inventory, false);
            BufferStorage?.ConnectStorage(player.Inventory, false);

            player.Inventory.ConnectStorage(InputStorage, false);
            player.Panel.ConnectStorage(InputStorage, false);

            InputStorage.OnDataWasChanged += OnInputItemWasChanged;
            OutputStorage.OnDataWasChanged += OnInputItemWasChanged;
            FuelStorage.OnDataWasChanged += OnInputItemWasChanged;
            if (BufferStorage != null) BufferStorage.OnDataWasChanged += OnInputItemWasChanged;

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

        private static void OnCrafted(ProcessingComponent block)
        {
            if (!UIManager.IsOpen("resourceProcessing")) return;
            if (!string.IsNullOrEmpty(GameAssets.GetBlockDataById(block.Id).FinishedSound)) return;

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
                status = "Status: Processing";
            }
            else
            {
                bool isValidIngredient = false;
                bool hasEnoughQuantity = false;

                if (inputSlot.HasItem && GameAssets.Recipes.ContainsKey(processingBlock.RecipeType))
                {
                    foreach (var recipe in GameAssets.Recipes[processingBlock.RecipeType].Values)
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
            if (BufferStorage != null) BufferStorage.OnDataWasChanged -= OnInputItemWasChanged;

            InputStorage = new Storage(1, 1);
            FuelStorage = new Storage(1, 1);
            OutputStorage = new Storage(1, 1);
            BufferStorage = null;
            processingBlock = null;
            Player = null;
            _wasVisible = false;
        }

        public static bool IsOpen => UIManager.IsOpen("resourceProcessing") && processingBlock != null;
        public static string Status => status;

        public static float Progress => processingBlock != null && processingBlock.IsRunning ? processingBlock.GetTaskProgress() / 100f : 0f;
        public static ItemSlot InputSlot => InputStorage.GetSlot(0, 0);
        public static ItemSlot OutputSlot => OutputStorage.GetSlot(0, 0);
        public static AtlasSprite InputIcon => inputIcon;
        public static AtlasSprite OutputIcon => outputIcon;
        public static ProcessingComponent Block => processingBlock;
        public static Storage Buffer => BufferStorage;

        public static IEnumerable<Recipe> Recipes
        {
            get
            {
                if (processingBlock == null || !GameAssets.Recipes.TryGetValue(processingBlock.RecipeType, out var recipes)) return Array.Empty<Recipe>();
                return recipes.Values;
            }
        }

        public static float RecipeSeconds(Recipe recipe) => processingBlock == null ? 0f : processingBlock.GetTicksWithEfficiency(recipe.RequiredTicks) / 20f;

        public static void Update()
        {
            bool isVisible = UIManager.IsOpen("resourceProcessing");

            if (isVisible != _wasVisible)
            {
                if (!isVisible)
                {
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

            if (isVisible && processingBlock != null && processingBlock.IsRunning)
                status = "Status: Processing";
        }

        public static void MoveItems(ItemSlot slot)
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
            if (moved <= 0 || Player == null || !IsProductSlot(slot, item)) return;

            Player.PlayerStatistics.AddProcessedItems(moved);
            if (item != null) PlayerActivity.Raise(PlayerActivityKind.ItemProcessed, item.Id, moved, processingBlock?.RecipeType);
        }

        private static bool IsProductSlot(ItemSlot slot, Item item)
        {
            if (slot.Storage == OutputStorage) return true;
            if (BufferStorage == null || slot.Storage != BufferStorage || item == null || processingBlock == null) return false;
            return !GameAssets.TryGetRecipe(processingBlock.RecipeType, item.Id, out _);
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