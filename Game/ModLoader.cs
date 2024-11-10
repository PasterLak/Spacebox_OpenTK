using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public static class ModLoader
    {
        public static void Load(string modName)
        {
            string modsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
            string defaultModName = "Default";
            string defaultModPath = Path.Combine(modsDirectory, defaultModName);

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                Debug.Error("Mods directory created. Please add mods and try again.");
                return;
            }

            string modPath = Path.Combine(modsDirectory, modName);
            if (!Directory.Exists(modPath))
            {
                Debug.Error($"Mod '{modName}' not found in Mods directory.");
                return;
            }

            
            string configFile = Path.Combine(modPath, "config.json");
            
            if (!File.Exists(configFile))
            {
                Debug.Error($"Config file not found for mod '{modName}'.");
                return;
            }

            ModConfig config = LoadConfig(configFile);
            if (config == null)
            {
                Debug.Error($"Failed to load config for mod '{modName}'.");
                return;
            }

            

            LoadTextures(modPath, defaultModPath, config.Textures);
            LoadBlocks(modPath, defaultModPath);
            LoadItems(modPath, defaultModPath);
            LoadSettings(modPath);
            LoadOptionalFiles(modPath);
            Debug.Log($"Mod '{modName}' loaded successfully.");
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

                GameBlocks.RegisterBlock(new BlockData("Air", new Vector2Byte(0, 0)));

                foreach (var block in blocks)
                {
                   
                    var c = block.LightColor;

                    bool sameSides = false;

                    if (block.Top == Vector2Byte.Zero) sameSides = true;
                    if (block.Bottom == Vector2Byte.Zero) sameSides = true;

                    if(block.Walls == block.Bottom && block.Walls == block.Top)
                        sameSides = true;


                    BlockData blockData = new BlockData(block.Name, block.Walls, 
                        block.IsTransparent, new Vector3(c.X/255f,c.Y/255f, c.Z/255f ))
                    {
                       
                        
                    };

                  
                        blockData.AllSidesAreSame = sameSides;

                    if(sameSides)
                    {
                        blockData.TopUVIndex = block.Walls;
                        blockData.BottomUVIndex = block.Walls;
                    }
                    else
                    {
                        blockData.TopUVIndex = block.Top;
                        blockData.BottomUVIndex = block.Bottom;
                    }
                

                    //blockData.CacheUVsByDirection();
                    GameBlocks.RegisterBlock(blockData);

                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading blocks: {ex.Message}");
            }
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
                        var newItem = new WeaponeItem((byte)item.MaxStack, item.Name,
                            item.TextureCoord.X, item.TextureCoord.Y, item.ModelDepth);

                        GameBlocks.RegisterItem(newItem);
                    }
                    else if (type == "drill")
                    {
                        var newItem = new DrillItem((byte)item.MaxStack, item.Name,
                           item.TextureCoord.X, item.TextureCoord.Y, item.ModelDepth);

                        GameBlocks.RegisterItem(newItem);
                    }
                    else if(type == "item")
                    {

                        var newItem = new Item((byte)item.MaxStack, item.Name,
                            item.TextureCoord.X, item.TextureCoord.Y, item.ModelDepth);

                        GameBlocks.RegisterItem(newItem);
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
                    // Implement handling for additional file types if needed
                }
            }
        }

        private static void ApplySettings(ModSettings settings)
        {
            // Implement settings application logic as needed
        }

        private class ModConfig
        {
            public string ModeName { get; set; }
            public string Author { get; set; }
            public string Version { get; set; }
            public List<TextureConfig> Textures { get; set; }
        }

        private class TextureConfig
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
        }

        private class ModBlockData // all data = 600 lines, without all sides = 500, optional param = 300 
        {

            public string Name { get; set; } = "NoName";
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
            public string Type { get; set; } = "Item";// "Item" or "BlockItem"
            public Vector2Byte TextureCoord { get; set; } = Vector2Byte.Zero;
            public short BlockId { get; set; } = 0; // For BlockItem
            public int MaxStack { get; set; } = 1;
            public float ModelDepth { get; set; } = 1.0f;
        }

        private class ModSettings
        {
            public Dictionary<string, string> Settings { get; set; }
        }
    }
}
