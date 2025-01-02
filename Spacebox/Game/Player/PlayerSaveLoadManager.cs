
using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Player
{
    public static class PlayerSaveLoadManager
    {
        public static void SavePlayer(Astronaut player, string worldFolder)
        {
            try
            {
                string saveFilePath = Path.Combine(worldFolder, "player.json");

                if (!Directory.Exists(worldFolder))
                {
                    Directory.CreateDirectory(worldFolder);
                }

                PlayerData data = new PlayerData
                {
                    PositionX = player.Position.X,
                    PositionY = player.Position.Y,
                    PositionZ = player.Position.Z,
                    RotationX = player.GetRotation().X,
                    RotationY = player.GetRotation().Y,
                    RotationZ = player.GetRotation().Z,
                    RotationW = player.GetRotation().W,
                    Health = player.HealthBar.StatsData.Count,
                    Power = player.PowerBar.StatsData.Count,
                    IsFlashlightOn = player.Flashlight.IsActive,
                    InventorySlots = new List<SavedItemSlot>(),
                    PanelSlots = new List<SavedItemSlot>()
                };

                foreach (var slot in player.Inventory.GetAllSlots())
                {
                    if (slot.HasItem)
                    {
                        data.InventorySlots.Add(new SavedItemSlot
                        {
                            ItemId = slot.Item.Id,
                            Count = slot.Count,
                            SlotX = (byte)slot.Position.X,
                            SlotY = (byte)slot.Position.Y
                        });
                    }
                }

                foreach (var slot in player.Panel.GetAllSlots())
                {
                    if (slot.HasItem)
                    {
                        data.PanelSlots.Add(new SavedItemSlot
                        {
                            ItemId = slot.Item.Id,
                            Count = slot.Count,
                            SlotX = (byte)slot.Position.X,
                            SlotY = (byte)slot.Position.Y
                        });
                    }
                }

                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(saveFilePath, jsonString);
                Debug.Log($"Player data saved at {saveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving player data: {ex.Message}");
            }
        }

        public static void LoadPlayer(Astronaut player, string worldFolder)
        {
            return;
            try
            {
                string saveFilePath = Path.Combine(worldFolder, "player.json");

                if (!File.Exists(saveFilePath))
                {
                    Debug.Log("Player save file does not exist.");

                    GameSetLoader.GiveStartItems(player, GameBlocks.Item);

                    return;
                }

                string jsonString = File.ReadAllText(saveFilePath);
                PlayerData data = JsonSerializer.Deserialize<PlayerData>(jsonString);

                if (data == null)
                {
                    Debug.Error("Failed to deserialize player data.");
                    return;
                }

                player.Position = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
                Quaternion loadedRotation = new Quaternion(data.RotationX, data.RotationY, data.RotationZ, data.RotationW);

                player.SetRotation(loadedRotation);
                player.HealthBar.StatsData.Count = data.Health;
                player.PowerBar.StatsData.Count = data.Power;

                player.Inventory.Clear();
                player.Panel.Clear();
                player.Flashlight.IsActive = data.IsFlashlightOn;

                foreach (var savedSlot in data.InventorySlots)
                {
                    if (GameBlocks.Item.TryGetValue(savedSlot.ItemId, out var item))
                    {
                        var slot = player.Inventory.GetSlot(savedSlot.SlotX, savedSlot.SlotY);
                        if (slot != null)
                        {
                            slot.Item = item;
                            slot.Count = savedSlot.Count;
                        }
                        else
                        {
                            Debug.Error($"Invalid slot coordinates ({savedSlot.SlotX}, {savedSlot.SlotY}) in Inventory.");
                        }
                    }
                    else
                    {
                        Debug.Error($"Item ID {savedSlot.ItemId} not found. Skipping loading into Inventory.");
                    }
                }

                foreach (var savedSlot in data.PanelSlots)
                {
                    if (GameBlocks.Item.TryGetValue(savedSlot.ItemId, out var item))
                    {
                        var slot = player.Panel.GetSlot(savedSlot.SlotX, savedSlot.SlotY);
                        if (slot != null)
                        {
                            slot.Item = item;
                            slot.Count = savedSlot.Count;
                        }
                        else
                        {
                            Debug.Error($"Invalid slot coordinates ({savedSlot.SlotX}, {savedSlot.SlotY}) in Panel.");
                        }
                    }
                    else
                    {
                        Debug.Error($"Item ID {savedSlot.ItemId} not found. Skipping loading into Panel.");
                    }
                }

                Debug.Success("Player data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading player data: {ex.Message}");
            }
        }

        private class PlayerData
        {
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }
            public float RotationX { get; set; }
            public float RotationY { get; set; }
            public float RotationZ { get; set; }
            public float RotationW { get; set; } = 1;
            public int Health { get; set; } = 100;
            public int Power { get; set; } = 100;
            public bool IsFlashlightOn { get; set; } = false;
            public List<SavedItemSlot> InventorySlots { get; set; }
            public List<SavedItemSlot> PanelSlots { get; set; }
        }

        private class SavedItemSlot
        {
            public short ItemId { get; set; }
            public byte Count { get; set; }
            public byte SlotX { get; set; }
            public byte SlotY { get; set; }
        }
    }

    public static class StorageExtensions
    {
        public static IEnumerable<ItemSlot> GetAllSlots(this Storage storage)
        {
            for (int x = 0; x < storage.SizeX; x++)
            {
                for (int y = 0; y < storage.SizeY; y++)
                {
                    yield return storage.GetSlot(x, y);
                }
            }
        }
    }
}
