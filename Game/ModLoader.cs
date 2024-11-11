using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public static class ModLoader
    {
        public static ModConfig ModInfo;
        public static void Load(string modId)
        {
            string modsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
            const string defaultModId = "Default";
            string defaultModPath = Path.Combine(modsDirectory, "Default");

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                Debug.Error("Mods directory created. Please add mods and try again.");
                return;
            }

            string modPath = modId.ToLower() == defaultModId.ToLower() ? defaultModPath : FindModPath(modsDirectory, modId);

            if (modPath == string.Empty)
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
            foreach (var directory in Directory.GetDirectories(modsDirectory))
            {
                string configPath = Path.Combine(directory, "config.json");
               
                if (File.Exists(configPath))
                {
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
                        GameBlocks.BlocksTexture = loadedTexture;
                        break;
                    case "items":
                        GameBlocks.ItemsTexture = loadedTexture;
                        break;
                    case "lightatlas":
                        GameBlocks.LightAtlas = loadedTexture;
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
            string blocksFile = Path.Combine(modPath, "blocks.json");
            string defaultBlocksFile = Path.Combine(defaultModPath, "blocks.json");

            if (!File.Exists(blocksFile))
            {
                blocksFile = defaultBlocksFile;
                Debug.Error("blocks.json not found in mod. Using default blocks.");
                if (!File.Exists(blocksFile))
                {
                    Debug.Error("Default blocks.json not found.");
                    return;
                }
            }

            try
            {
                string json = File.ReadAllText(blocksFile);
                List<ModBlockData> blocks = JsonSerializer.Deserialize<List<ModBlockData>>(json);

                GameBlocks.RegisterBlock(new BlockData("Air", "Block", new Vector2Byte(0, 0)));

                foreach (var block in blocks)
                {
                    block.Type = block.Type.ToLower();

                    if (!ValidateBlockType(block.Type))
                    {
                        Debug.Error($"[GameSetLoader] Block '{block.Name}' has an invalid type and was skipped" );
                        Debug.Error($"[GameSetLoader] Valid types are: {BlockTypesToString()}");
                        continue;
                    }
                    var c = block.LightColor;

                    bool sameSides = false;

                    if (block.Top == Vector2Byte.Zero) sameSides = true;
                    if (block.Bottom == Vector2Byte.Zero) sameSides = true;

                    if (block.Walls == block.Bottom && block.Walls == block.Top)
                        sameSides = true;

                    bool hasLightColor = !(c.X == 0 && c.Y == 0 && c.Z == 0);

                    var blockColor = hasLightColor ? new Vector3(c.X / 255f, c.Y / 255f, c.Z / 255f) : Vector3.Zero;

                    BlockData blockData = new BlockData(block.Name, block.Type, block.Walls, block.IsTransparent, blockColor)
                    {
                        AllSidesAreSame = sameSides
                    };

                    if (sameSides)
                    {
                        blockData.TopUVIndex = block.Walls;
                        blockData.BottomUVIndex = block.Walls;
                    }
                    else
                    {
                        blockData.TopUVIndex = block.Top;
                        blockData.BottomUVIndex = block.Bottom;
                    }

                    GameBlocks.RegisterBlock(blockData);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading blocks: {ex.Message}");
            }
        }
        private static string[] BlockTypes = { "block", "interactive", "door" };

        private static string BlockTypesToString()
        {
            string types = "";

            for (int i = 0; i < BlockTypes.Length; i++)
            {
                types += BlockTypes[i];
                if(i < BlockTypes.Length - 1) types += ", ";
            }

            return types;
        }
        private static bool ValidateBlockType(string type)
        {
            foreach (string t in BlockTypes)
            {
                if(t.ToLower() == type.ToLower()) return true;
            }

            return false;
        }

        private static void LoadItems(string modPath, string defaultModPath)
        {
            string itemsFile = Path.Combine(modPath, "items.json");
            string defaultItemsFile = Path.Combine(defaultModPath, "items.json");

            if (!File.Exists(itemsFile))
            {
                itemsFile = defaultItemsFile;
                Debug.Error("items.json not found in mod. Using default items.");
                if (!File.Exists(itemsFile))
                {
                    Debug.Error("Default items.json not found.");
                    return;
                }
            }

            try
            {
                string json = File.ReadAllText(itemsFile);
                List<ModItemData> items = JsonSerializer.Deserialize<List<ModItemData>>(json);
                foreach (var item in items)
                {
                    string type = item.Type.ToLower();

                    if (type == "weapone")
                    {
                        var newItem = new WeaponeItem((byte)item.MaxStack, item.Name, item.TextureCoord.X, item.TextureCoord.Y, item.ModelDepth);
                        GameBlocks.RegisterItem(newItem);
                    }
                    else if (type == "drill")
                    {
                        var newItem = new DrillItem((byte)item.MaxStack, item.Name, item.TextureCoord.X, item.TextureCoord.Y, item.ModelDepth);
                        GameBlocks.RegisterItem(newItem);
                    }
                    else if (type == "consumable")
                    {
                        var newItem = new ConsumableItem((byte)item.MaxStack, item.Name, item.TextureCoord.X, item.TextureCoord.Y, item.ModelDepth);
                        GameBlocks.RegisterItem(newItem);
                    }
                    else if (type == "item")
                    {
                        var newItem = new Item((byte)item.MaxStack, item.Name, item.TextureCoord.X, item.TextureCoord.Y, item.ModelDepth);
                        GameBlocks.RegisterItem(newItem);
                    }
                    else
                    {
                        Debug.Error($"Unknown item type '{item.Type}' for item '{item.Name}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading items: {ex.Message}");
            }
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
                    string extension = Path.GetExtension(file).ToLower();

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
                        Common.Lighting.AmbientColor = new Vector3(settings.AmbientColor.X/255f, settings.AmbientColor.Y / 255f, settings.AmbientColor.Z / 255f);
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
        }

        public class ModConfig
        {
            public string ModId { get; set; } = "default";
            public string ModeName { get; set; } = "Default mod name";
            public string Description { get; set; } = "";
            public string Author { get; set; } = "";
            public string Version { get; set; } = "0";
            public int BlockSize { get; set; } = 32;
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
            public string Type { get; set; }
            public string Name { get; set; } = "NoName Texture";
            public string Path { get; set; }
        }

        private class ModBlockData
        {
            public string Name { get; set; } = "NoName";
            public string Type { get; set; } = "block";
            public Vector2Byte Walls { get; set; } = new Vector2Byte(0, 0);
            public Vector2Byte Top { get; set; } = new Vector2Byte(0, 0);
            public Vector2Byte Bottom { get; set; } = new Vector2Byte(0, 0);
            public bool IsTransparent { get; set; } = false;
            public Vector3Byte LightColor { get; set; } = Vector3Byte.Zero;
        }

        private class ModItemData
        {
            public string Info { get; set; } = "";
            public string Name { get; set; } = "NoName";
            public string Type { get; set; } = "Item";
            public Vector2Byte TextureCoord { get; set; } = Vector2Byte.Zero;
            public short BlockId { get; set; } = 0;
            public int MaxStack { get; set; } = 1;
            public float ModelDepth { get; set; } = 1.0f;
        }

        private class ModSettings
        {
            public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        }
    }
}
