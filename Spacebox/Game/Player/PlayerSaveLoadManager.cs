using System.Text.Json;
using OpenTK.Mathematics;

using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Resource;
using Engine;
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
                player.PlayerStatistics.EndSession();

                PlayerData data = new PlayerData
                {
                    PositionX = player.Position.X,
                    PositionY = player.Position.Y,
                    PositionZ = player.Position.Z,
                    RotationX = player.GetRotation().X,
                    RotationY = player.GetRotation().Y,
                    RotationZ = player.GetRotation().Z,
                    RotationW = player.GetRotation().W,
                    Health = player.HealthBar.StatsData.Value,
                    Power = player.PowerBar.StatsData.Value,
                    IsFlashlightOn = player.Flashlight.Enabled,
                    InventorySlots = new List<SavedItemSlot>(),
                    PanelSlots = new List<SavedItemSlot>(),
                    Statistics = player.PlayerStatistics
                };

                foreach (var slot in player.Inventory.GetAllSlots())
                {
                    if (slot.HasItem)
                    {
                        data.InventorySlots.Add(new SavedItemSlot
                        {
                            ItemID = slot.Item.Id_string,
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
                            ItemID = slot.Item.Id_string,
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

            }
            catch (Exception ex)
            {
                Debug.Error($"[PlayerSaveLoadManager] Error saving player data: {ex.Message}");
            }
        }

        public static void LoadPlayer(Astronaut player, string worldFolder)
        {

            try
            {
                string saveFilePath = Path.Combine(worldFolder, "player.json");

                if (!File.Exists(saveFilePath))
                {
                    Debug.Log("[PlayerSaveLoadManager] Player save file does not exist.");

                    World.CurrentSector.SpawnPlayerNearAsteroid(player, new Random(World.Seed));
                    GameSetLoader.GiveStartItems(player, GameAssets.ItemsStr);
                    player.Flashlight.Enabled = true;
                    player.PlayerStatistics.FirstPlayedUtc = DateTime.UtcNow;

                    return;
                }

                string jsonString = File.ReadAllText(saveFilePath);
                PlayerData data = JsonSerializer.Deserialize<PlayerData>(jsonString);

                if (data == null)
                {
                    Debug.Error("[PlayerSaveLoadManager] Failed to deserialize player data.");
                    return;
                }

                player.SetPosition( new Vector3(data.PositionX, data.PositionY, data.PositionZ));
                player.SpawnPosition = player.Position;
                Quaternion loadedRotation = new Quaternion(data.RotationX, data.RotationY, data.RotationZ, data.RotationW);

                player.SetRotation(loadedRotation);
                player.HealthBar.StatsData.Value = data.Health;
                player.PowerBar.StatsData.Value = data.Power;

                player.Inventory.Clear();
                player.Panel.Clear();
                player.Flashlight.Enabled = data.IsFlashlightOn;
                player.PlayerStatistics = data.Statistics;

                player.PlayerStatistics.StartSession();

                foreach (var savedSlot in data.InventorySlots)
                {
                    if (GameAssets.TryGetItemByFullID(savedSlot.ItemID, out var item))
                    {
                        var slot = player.Inventory.GetSlot(savedSlot.SlotX, savedSlot.SlotY);
                        if (slot != null)
                        {
                            slot.Item = item;
                            slot.Count = savedSlot.Count;
                        }
                        else
                        {
                            Debug.Error($"[PlayerSaveLoadManager] Invalid slot coordinates ({savedSlot.SlotX}, {savedSlot.SlotY}) in Inventory.");
                        }
                    }
                    else
                    {
                        Debug.Error($"[PlayerSaveLoadManager] Item ID {savedSlot.ItemID} not found. Skipping loading into Inventory.");
                    }
                }

                foreach (var savedSlot in data.PanelSlots)
                {
                    if (GameAssets.TryGetItemByFullID(savedSlot.ItemID, out var item))
                    {
                        var slot = player.Panel.GetSlot(savedSlot.SlotX, savedSlot.SlotY);
                        if (slot != null)
                        {
                            slot.Item = item;
                            slot.Count = savedSlot.Count;
                        }
                        else
                        {
                            Debug.Error($"[PlayerSaveLoader] Invalid slot coordinates ({savedSlot.SlotX}, {savedSlot.SlotY}) in Panel. Trying to add the item to the Inventory: ");

                            if (player.Inventory.TryAddItem(item, savedSlot.Count))
                            {
                                Debug.Success($"The item was added to Inventory!");
                            }
                            else
                            {
                                Debug.Error($"[PlayerSaveLoadManager] Was not enough free space in the Inventory!");
                            }
                        }
                    }
                    else
                    {
                        Debug.Error($"[PlayerSaveLoadManager] Item ID {savedSlot.ItemID} not found. Skipping loading into Panel.");
                    }
                }


                PanelUI.SetSelectedSlot(0);
            }
            catch (Exception ex)
            {
                Debug.Error($"[PlayerSaveLoadManager] Error loading player data: {ex.Message}");
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

            public PlayerStatistics Statistics { get; set; } = new PlayerStatistics();
        }

        private class SavedItemSlot
        {
            public string ItemID { get; set; }
            public byte Count { get; set; }
            public byte SlotX { get; set; }
            public byte SlotY { get; set; }
        }
    }

}
