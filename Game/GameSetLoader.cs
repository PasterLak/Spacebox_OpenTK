using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public static class GameSetLoader
    {
        public static ModConfig ModInfo;

        public static void Load(string modId)
        {
            string modsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.GameSet.Folder);
            string defaultModId = Globals.GameSet.Default.ToLower();
            string defaultModPath = Path.Combine(modsDirectory, Globals.GameSet.Default);

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                Debug.Error("Mods directory created. Please add mods and try again.");
                return;
            }

            string modPath = modId.ToLower() == defaultModId ? defaultModPath : FindModPath(modsDirectory, modId);

            if (string.IsNullOrEmpty(modPath))
            {
                Debug.Error($"Mod with ID '{modId}' not found in Mods directory.");
                return;
            }

            string configFile = Path.Combine(modPath, "config.json");

            if (!File.Exists(configFile))
            {
                Debug.Error($"Config file not found for mod '{modId}'.");
                return;
            }

            ModInfo = LoadConfig(configFile);
            if (ModInfo == null)
            {
                Debug.Error($"Failed to load config for mod '{modId}'.");
                return;
            }

            LoadTextures(modPath, defaultModPath, ModInfo.Textures);
            LoadBlocks(modPath, defaultModPath);
            LoadItems(modPath, defaultModPath);
            LoadSettings(modPath);
            LoadOptionalFiles(modPath);

            Debug.Success($"Mod '{modId}' loaded successfully.");
        }

        private static string FindModPath(string modsDirectory, string modId)
        {
            modId = modId.ToLower();
            foreach (var directory in Directory.GetDirectories(modsDirectory))
            {
                string configPath = Path.Combine(directory, "config.json");
                if (!File.Exists(configPath)) continue;

                try
                {
                    string json = File.ReadAllText(configPath);
                    ModConfig config = JsonSerializer.Deserialize<ModConfig>(json);
                    if (config != null && config.ModId.ToLower() == modId)
                    {
                        return directory;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Error($"Error reading config from '{directory}': {ex.Message}");
                }
            }
            return string.Empty;
        }

        private static ModConfig LoadConfig(string configFile)
        {
            try
            {
                string json = File.ReadAllText(configFile);
                return JsonSerializer.Deserialize<ModConfig>(json);
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading config: {ex.Message}");
                return null;
            }
        }

        private static void LoadTextures(string modPath, string defaultModPath, List<TextureConfig> textures)
        {
            return;

            foreach (var texture in textures)
            {
                string texturePath = Path.Combine(modPath, texture.Path);
                string fallbackPath = Path.Combine(defaultModPath, texture.Path);

                if (!File.Exists(texturePath))
                {
                    if (File.Exists(fallbackPath))
                    {
                        texturePath = fallbackPath;
                        Debug.Log($"Texture '{texture.Name}' not found in mod. Using default texture.");
                    }
                    else
                    {
                        Debug.Error($"Texture '{texture.Name}' not found in mod and default mod.");
                        continue;
                    }
                }

                Texture2D loadedTexture = new Texture2D(texturePath, true);
                switch (texture.Type.ToLower())
                {
                    case "blocks":
                        //loadedTexture.SaveToPng("blocks.png");
                        break;
                    case "items":
                        //GameBlocks.ItemsTexture = loadedTexture;
                        break;
                    case "lightatlas":
                        //GameBlocks.LightAtlas = loadedTexture;
                        break;
                    case "dust":
                        GameBlocks.DustTexture = loadedTexture;
                        break;
                    default:
                        Debug.Error($"Unknown texture type '{texture.Type}' for texture '{texture.Name}'.");
                        break;
                }
            }
        }

        private static void LoadBlocks(string modPath, string defaultModPath)
        {
            string blocksFile = GetFilePath(modPath, defaultModPath, "blocks.json");
            if (blocksFile == null) return;

            try
            {
                string json = File.ReadAllText(blocksFile);
          
                List<ModBlockData> blocks = JsonSerializer.Deserialize<List<ModBlockData>>(json);

                

                var air = new BlockData("Air", "block", new Vector2Byte(0, 0));
          
                air.Sides = "sand";
             
                GameBlocks.RegisterBlock(air);
         
                foreach (var block in blocks)
                {
                    block.Type = block.Type.ToLower();
                    block.Sides = block.Sides.ToLower();
                    block.Top = block.Top.ToLower();
                    block.Bottom = block.Bottom.ToLower();

                    if (!ValidateBlockType(block.Type))
                    {
                        Debug.Error($"Block '{block.Name}' has an invalid type and was skipped");
                        Debug.Error($"Valid types are: {string.Join(", ", BlockTypes)}");
                        continue;
                    }

                    bool sameSides = block.Top == "" || block.Top == "" || (block.Sides == block.Top && block.Sides == block.Top);
                    bool hasLightColor = block.LightColor != Vector3Byte.Zero;
                    var blockColor = hasLightColor ? new Vector3(block.LightColor.X / 255f, block.LightColor.Y / 255f, block.LightColor.Z / 255f) : Vector3.Zero;

                    BlockData blockData = new BlockData(block.Name, block.Type, new Vector2Byte(0,0), block.IsTransparent, blockColor)
                    {
                        AllSidesAreSame = sameSides,
                        TopUVIndex = new Vector2Byte(),
                        BottomUVIndex = new Vector2Byte(),

                    };

                    blockData.Sides = block.Sides;
                    blockData.Top = sameSides ? block.Sides : block.Top;
                    blockData.Bottom = sameSides ? block.Sides : block.Bottom;

                    GameBlocks.RegisterBlock(blockData);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading blocks: {ex.Message}");
            }
        }

        private static string[] BlockTypes = { "block", "interactive", "door", "light" };

        private static bool ValidateBlockType(string type)
        {
            return BlockTypes.Contains(type.ToLower());
        }

        private static void LoadItems(string modPath, string defaultModPath)
        {
            string itemsFile = GetFilePath(modPath, defaultModPath, "items.json");
            if (itemsFile == null) return;

            try
            {
                string json = File.ReadAllText(itemsFile);
                JsonDocument jsonDoc = JsonDocument.Parse(json);
                JsonElement root = jsonDoc.RootElement;

                foreach (JsonElement itemElement in root.EnumerateArray())
                {
                    string type = "item";
                  

                    if (itemElement.TryGetProperty("Type", out JsonElement typeElement))
                    {
                        type = typeElement.GetString().ToLower();
                    }
                   



                    switch (type)
                    {
                        case "weapon":
                            var weaponData = itemElement.Deserialize<WeaponItemData>();
                            if (weaponData == null) continue;
                            RegisterWeaponItem(weaponData);
                            break;

                        case "drill":
                            var drillData = itemElement.Deserialize<DrillItemData>();
                            if (drillData == null) continue;
                            RegisterDrillItem(drillData);
                            break;

                        case "consumable":
                            var consumableData = itemElement.Deserialize<ConsumableItemData>();
                            if (consumableData == null) continue;
                            RegisterConsumableItem(consumableData);
                            break;

                        case "item":
                        default:
                            var itemData = itemElement.Deserialize<ModItemData>();
                            if (itemData == null) continue;
                            RegisterItem(itemData);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading items: {ex.Message}");
            }
        }

        private static void RegisterWeaponItem(WeaponItemData data)
        {
            data.MaxStack = 1;
            var weaponItem = new WeaponItem(
                (byte)data.MaxStack,
                data.Name,
             
                data.ModelDepth)
            {
                Damage = data.Damage
            };
            
            GameBlocks.RegisterItem(weaponItem, data.Sprite);
        }

        private static void RegisterDrillItem(DrillItemData data)
        {
            data.MaxStack = 1;
            var drillItem = new DrillItem(
                (byte)data.MaxStack,
                data.Name,
            
                data.ModelDepth)
            {
                Power = data.Power
            };
            GameBlocks.RegisterItem(drillItem, data.Sprite);
        }

        private static void RegisterConsumableItem(ConsumableItemData data)
        {
            data.ValidateMaxStack();
            var consumableItem = new ConsumableItem(
                (byte)data.MaxStack,
                data.Name,
            
                data.ModelDepth)
            {
                HealAmount = data.HealAmount
            };
            GameBlocks.RegisterItem(consumableItem, data.Sprite);
        }

        private static void RegisterItem(ModItemData data)
        {
            data.ValidateMaxStack();
            var item = new Item(
                (byte)data.MaxStack,
                data.Name,
            
                data.ModelDepth);
            GameBlocks.RegisterItem(item, data.Sprite);
        }

        private static void LoadSettings(string modPath)
        {
            string settingsFile = Path.Combine(modPath, "settings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    string json = File.ReadAllText(settingsFile);
                    ModSettings settings = JsonSerializer.Deserialize<ModSettings>(json);
                    ApplySettings(settings);
                }
                catch (Exception ex)
                {
                    Debug.Error($"Error loading settings: {ex.Message}");
                }
            }
        }

        private static void LoadOptionalFiles(string modPath)
        {
            string optionalPath = Path.Combine(modPath, "Optional");

            if (Directory.Exists(optionalPath))
            {
                foreach (string file in Directory.GetFiles(optionalPath))
                {
                    if (Path.GetFileNameWithoutExtension(file).Equals("lighting", StringComparison.OrdinalIgnoreCase))
                    {
                        LoadLighting(optionalPath);
                        Debug.Log("Lighting file was loaded!");
                    }
                }
            }
        }

        private static void LoadLighting(string path)
        {
            string file = Path.Combine(path, "lighting.json");
            if (File.Exists(file))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    Modlighting settings = JsonSerializer.Deserialize<Modlighting>(json);

                    if (settings != null)
                    {
                        Common.Lighting.AmbientColor = new Vector3(settings.AmbientColor.X / 255f, settings.AmbientColor.Y / 255f, settings.AmbientColor.Z / 255f);
                        Common.Lighting.FogColor = new Vector3(settings.FogColor.X / 255f, settings.FogColor.Y / 255f, settings.FogColor.Z / 255f);
                        Common.Lighting.FogDensity = settings.FogDensity;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Error($"Error loading lighting settings: {ex.Message}");
                }
            }
        }

        private static void ApplySettings(ModSettings settings)
        {
            // Apply mod-specific settings here
        }

        private static string GetFilePath(string modPath, string defaultModPath, string fileName)
        {
            string filePath = Path.Combine(modPath, fileName);
            string defaultFilePath = Path.Combine(defaultModPath, fileName);

            if (!File.Exists(filePath))
            {
                filePath = defaultFilePath;
                Debug.Error($"{fileName} not found in mod. Using default.");
                if (!File.Exists(filePath))
                {
                    Debug.Error($"Default {fileName} not found.");
                    return null;
                }
            }
            return filePath;
        }

        public class ModConfig
        {
            public string ModId { get; set; } = "default";
            public string ModName { get; set; } = "Default Name";
            public string Description { get; set; } = "";
            public string Author { get; set; } = "";
            public string Version { get; set; } = "0";
            public int BlockSize { get; set; } = 32;
            public string FolderName { get; set; } = "";
            public List<TextureConfig> Textures { get; set; } = new List<TextureConfig>();
        }

        public class Modlighting
        {
            public Vector3Byte AmbientColor { get; set; } = Vector3Byte.One;
            public Vector3Byte FogColor { get; set; } = Vector3Byte.Zero;
            public float FogDensity { get; set; } = 0.05f;
        }

        public class TextureConfig
        {
            public string Type { get; set; } = "unknown";
            public string Name { get; set; } = "NoName Texture";
            public string Path { get; set; } = "";
        }


        private class ModSettings
        {
            public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        }
    }
}
