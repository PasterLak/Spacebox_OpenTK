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
        private const string PlayersFolder = "players";
        private const string LegacyFileName = "player.json";
        private const string LocalFileName = "localplayer.json";

        public static string GetPlayerFilePath(string worldFolder, string playerName)
        {
            var safeName = new string((playerName ?? "Player").Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            return Path.Combine(worldFolder, PlayersFolder, safeName + ".json");
        }

        public static string ReadPlayerJson(string worldFolder, string playerName, bool allowLegacy)
        {
            string path = GetPlayerFilePath(worldFolder, playerName);
            if (File.Exists(path)) return File.ReadAllText(path);

            string legacy = Path.Combine(worldFolder, LegacyFileName);
            return allowLegacy && File.Exists(legacy) ? File.ReadAllText(legacy) : null;
        }

        public static string GetLocalPlayerFilePath(string worldFolder)
        {
            return Path.Combine(worldFolder, LocalFileName);
        }

        public static string ReadLocalPlayerJson(string worldFolder, string playerName)
        {
            string path = GetLocalPlayerFilePath(worldFolder);
            return File.Exists(path) ? File.ReadAllText(path) : ReadPlayerJson(worldFolder, playerName, true);
        }

        public static void WriteLocalPlayerJson(string worldFolder, string json)
        {
            if (json == null) return;

            try
            {
                Directory.CreateDirectory(worldFolder);
                File.WriteAllText(GetLocalPlayerFilePath(worldFolder), json);
            }
            catch (Exception ex)
            {
                Debug.Error($"[PlayerSaveLoadManager] Error saving local player: {ex.Message}");
            }
        }

        public static void WritePlayerJson(string worldFolder, string playerName, string json)
        {
            try
            {
                string path = GetPlayerFilePath(worldFolder, playerName);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.Error($"[PlayerSaveLoadManager] Error saving player '{playerName}': {ex.Message}");
            }
        }

        public static void SavePlayer(LocalAstronaut player, string worldFolder)
        {
            WriteLocalPlayerJson(worldFolder, Serialize(player));
        }

        public static string Serialize(LocalAstronaut player)
        {
            try
            {
                player.PlayerStatistics.PrepareForSave();
                Spacebox.Game.Tutorial.TutorialService.SyncProgress();
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
                    Oxygen = player.OxygenBar.StatsData.Value,
                    IsFlashlightOn = player.Flashlight.Enabled,
                    SpawnPoint = new[] { player.SpawnPosition.X, player.SpawnPosition.Y, player.SpawnPosition.Z },
                    InventorySlots = CollectSlots(player.Inventory),
                    PanelSlots = CollectSlots(player.Panel),
                    EquipmentSlots = CollectSlots(player.Equipment.Storage),
                    Statistics = player.PlayerStatistics,
                    Tutorial = player.Tutorial
                };

                return JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                Debug.Error($"[PlayerSaveLoadManager] Error serializing player data: {ex.Message}");
                return null;
            }
        }

        public static void LoadPlayer(LocalAstronaut player, string worldFolder)
        {
            string json = null;
            try
            {
                json = ReadLocalPlayerJson(worldFolder, Spacebox.Network.GameSession.PlayerName);
            }
            catch (Exception ex)
            {
                Debug.Error($"[PlayerSaveLoadManager] Error reading player data: {ex.Message}");
            }

            LoadPlayerFromJson(player, json);
        }

        public static void LoadPlayerFromJson(LocalAstronaut player, string json)
        {
            if (string.IsNullOrEmpty(json) || !Apply(player, json))
                ApplyNewPlayer(player);
        }

        public static void ApplyNewPlayer(LocalAstronaut player)
        {
            Debug.Log("[PlayerSaveLoadManager] New player, spawning near an asteroid.");

            if (Respawn.WorldSpawn.TryGet(out var worldSpawn))
            {
                player.SetPosition(Respawn.SafeSpawnFinder.FindFreePosition(worldSpawn));
                player.SpawnPosition = worldSpawn;
            }
            else
            {
                World.CurrentSector.SpawnPlayerNearRandomAsteroidData(player, new Random(World.Seed));
            }
            player.IsNewPlayer = true;
            player.Tutorial = null;
            GameSetLoader.GiveStartItems(player, GameAssets.ItemsStr);
            player.Flashlight.Enabled = true;
            player.PlayerStatistics = new PlayerStatistics();
            player.PlayerStatistics.StartSession();
        }

        private static bool Apply(LocalAstronaut player, string jsonString)
        {
            try
            {
                PlayerData data = JsonSerializer.Deserialize<PlayerData>(jsonString);

                if (data == null)
                {
                    Debug.Error("[PlayerSaveLoadManager] Failed to deserialize player data.");
                    return false;
                }

                player.SetPosition( new Vector3(data.PositionX, data.PositionY, data.PositionZ));
                player.SpawnPosition = data.SpawnPoint is { Length: 3 }
                    ? new Vector3(data.SpawnPoint[0], data.SpawnPoint[1], data.SpawnPoint[2])
                    : player.Position;
                Quaternion loadedRotation = new Quaternion(data.RotationX, data.RotationY, data.RotationZ, data.RotationW);

                player.SetRotation(loadedRotation);

                player.Inventory.Clear();
                player.Panel.Clear();
                player.Equipment.Storage.Clear();
                player.Flashlight.Enabled = data.IsFlashlightOn;
                player.PlayerStatistics = data.Statistics ?? new PlayerStatistics();
                player.IsNewPlayer = false;
                player.Tutorial = data.Tutorial;
                player.Tutorial?.Normalize();

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


                if (data.EquipmentSlots != null) LoadEquipment(player, data.EquipmentSlots);
                else GameSetLoader.GiveStartEquipment(player);
                if (data.Health > 0) player.HealthBar.StatsData.Value = data.Health;
                player.PowerBar.StatsData.Value = data.Power;
                player.OxygenBar.StatsData.Value = data.Oxygen;
                if (WasSavedDead(data.Health)) RespawnSavedDeadPlayer(player);

                PanelUI.SetSelectedSlot(0);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Error($"[PlayerSaveLoadManager] Error loading player data: {ex.Message}");
                return false;
            }
        }

        public static bool WasSavedDead(int health)
        {
            return health <= 0;
        }

        private static void RespawnSavedDeadPlayer(LocalAstronaut player)
        {
            Debug.Warning("[PlayerSaveLoadManager] The player was saved dead, respawning at the spawn point.");
            player.SetPosition(Respawn.SafeSpawnFinder.FindFreePosition(Respawn.WorldSpawn.For(player)));
            player.HealthBar.StatsData.Value = player.HealthBar.StatsData.MaxValue;
            player.PowerBar.StatsData.Value = player.PowerBar.StatsData.MaxValue;
            player.OxygenBar.StatsData.Fill();
        }

        private static List<SavedItemSlot> CollectSlots(Storage storage)
        {
            var slots = new List<SavedItemSlot>();

            foreach (var slot in storage.GetAllSlots())
            {
                if (!slot.HasItem) continue;

                slots.Add(new SavedItemSlot
                {
                    ItemID = slot.Item.Id_string,
                    Count = slot.Count,
                    SlotX = (byte)slot.Position.X,
                    SlotY = (byte)slot.Position.Y
                });
            }

            return slots;
        }

        private static void LoadEquipment(LocalAstronaut player, List<SavedItemSlot> savedSlots)
        {
            foreach (var savedSlot in savedSlots)
            {
                if (!GameAssets.TryGetItemByFullID(savedSlot.ItemID, out var item))
                {
                    Debug.Error($"[PlayerSaveLoadManager] Item ID {savedSlot.ItemID} not found. Skipping loading into Equipment.");
                    continue;
                }

                var slot = player.Equipment.Storage.GetSlot(savedSlot.SlotX, savedSlot.SlotY);
                if (slot != null && slot.CanAccept(item))
                {
                    slot.Item = item;
                    slot.Count = savedSlot.Count;
                }
                else if (!player.Inventory.TryAddItem(item, savedSlot.Count))
                {
                    Debug.Error($"[PlayerSaveLoadManager] Could not restore equipment item {savedSlot.ItemID}.");
                }
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
            public int Oxygen { get; set; } = 100;
            public bool IsFlashlightOn { get; set; } = false;
            public float[] SpawnPoint { get; set; }
            public List<SavedItemSlot> InventorySlots { get; set; }
            public List<SavedItemSlot> PanelSlots { get; set; }
            public List<SavedItemSlot> EquipmentSlots { get; set; }

            public PlayerStatistics Statistics { get; set; } = new PlayerStatistics();
            public Spacebox.Game.Tutorial.TutorialProgress Tutorial { get; set; }
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
