using System.Text.Json;
using DryIoc;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Game.Player;

namespace Spacebox.Game.Resources
{
    public static class GameSetLoader
    {
        public static ModConfig ModInfo;
        private static string modPath;
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

            modPath = modId.ToLower() == defaultModId ? defaultModPath : FindModPath(modsDirectory, modId);

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
            LoadSounds(modPath);
            LoadBlocks(modPath, defaultModPath);
            LoadItems(modPath, defaultModPath);
            LoadRecipes(modPath, defaultModPath);
            LoadCrafting(modPath, defaultModPath);
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

        private static void LoadSounds(string modPath)
        {
            var audioPath = Path.Combine(modPath, "Sounds");
            if (!Directory.Exists(audioPath)) return;

            try
            {
                string[] files = GetWavFilesRecursive(audioPath);
                foreach (string file in files)
                {
                    ProcessSoundFile(file);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading sounds: {ex.Message}");
            }
        }

        private static string[] GetWavFilesRecursive(string rootPath)
        {
            return Directory.GetFiles(rootPath, "*.wav", SearchOption.AllDirectories);
        }

        private static void ProcessSoundFile(string file)
        {
            var soundName = Path.GetFileNameWithoutExtension(file);
            if (!GameBlocks.Sounds.ContainsKey(soundName))
            {
                GameBlocks.Sounds.Add(soundName, new AudioClip(file));
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
                air.Mass = 0;
                air.Category = "";
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

                    bool sameSides = block.Top == "" || block.Top == "" || block.Sides == block.Top && block.Sides == block.Top;
                    bool hasLightColor = block.LightColor != Vector3Byte.Zero;
                    var blockColor = hasLightColor ? new Vector3(block.LightColor.X / 255f, block.LightColor.Y / 255f, block.LightColor.Z / 255f) : Vector3.Zero;

                    BlockData blockData = new BlockData(block.Name, block.Type, new Vector2Byte(0, 0), block.IsTransparent, blockColor)
                    {
                        AllSidesAreSame = sameSides,
                        TopUVIndex = new Vector2Byte(),
                        BottomUVIndex = new Vector2Byte(),

                    };

                    if (block.Durability <= 0) block.Durability = 1;
                    if (block.Mass <= 0) block.Mass = 1;
                    if (block.PowerToDrill <= 0) block.PowerToDrill = 1;

                    if (block.Mass <= byte.MaxValue)
                    {
                        blockData.Mass = (byte)block.Mass;
                    }
                    else
                    {
                        blockData.Mass = byte.MaxValue;
                    }

                    if (block.Durability <= byte.MaxValue)
                    {
                        blockData.Health = (byte)block.Durability;
                    }
                    else
                    {
                        blockData.Health = byte.MaxValue;
                    }

                    if (block.PowerToDrill <= byte.MaxValue)
                    {
                        blockData.PowerToDrill = (byte)block.PowerToDrill;
                    }
                    else
                    {
                        blockData.PowerToDrill = byte.MaxValue;
                    }
                    blockData.Efficiency = block.Efficiency;
                    blockData.Category = block.Category;
                    blockData.Sides = block.Sides;
                    blockData.Top = sameSides ? block.Sides : block.Top;
                    blockData.Bottom = sameSides ? block.Sides : block.Bottom;

                    GiveBlockSounds(blockData, block);

                    GameBlocks.RegisterBlock(blockData);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[GamesetLoader] Error loading blocks: {ex.Message}");
            }
        }

        private static void GiveBlockSounds(BlockData blockData, ModBlockData modBlockData)
        {
            if(!GameBlocks.Sounds.ContainsKey(modBlockData.SoundPlace))
            {
                blockData.SetDefaultPlaceSound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> has a wrong place sound! - {modBlockData.SoundPlace}. Selected a default one");
            }
            else
            {
                blockData.SoundPlace = modBlockData.SoundPlace;
            }
            if (!GameBlocks.Sounds.ContainsKey(modBlockData.SoundDestroy))
            {
                blockData.SetDefaultDestroySound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> has a wrong destroy sound! - {modBlockData.SoundDestroy}. Selected a default one");
            }
            else
            {
                blockData.SoundDestroy = modBlockData.SoundDestroy;
            }
        }

        private static string[] BlockTypes = { "block", "crusher", "furnace","radar" ,"craftingtable", "disassembler", "interactive", "door", "light" };

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
                            var itemData = itemElement.Deserialize<ItemData>();
                            if (itemData == null) continue;
                            RegisterItem(itemData);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[GamesetLoader] Error loading items: {ex.Message}");
            }
        }



        private static void LoadRecipes(string modPath, string defaultModPath)
        {
            string itemsFile = GetFilePath(modPath, defaultModPath, "recipes.json");
            if (itemsFile == null) return;

            try
            {
                string json = File.ReadAllText(itemsFile);

                List<RecipeData> recipes = JsonSerializer.Deserialize<List<RecipeData>>(json);

                foreach (var r in recipes)
                {
                    if (r == null) continue;

                    var item = GameBlocks.GetItemByName(r.Ingredient.Item);
                    var item2 = GameBlocks.GetItemByName(r.Product.Item);

                    if (item == null)
                    {
                        Debug.Error($"[GameSetLoader] Recipes: ingredient was not found - {r.Ingredient.Item} . This recipe was skipped");
                        continue;
                    }
                    if (item2 == null) continue;

                    if(item.Id == item2.Id)
                    {
                        Debug.Error($"[GameSetLoader] Recipes: product was not found - {r.Ingredient.Item} . This recipe was skipped");
                        continue;
                    }

                    GameBlocks.RegisterRecipe(r, item, item2);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Error loading recipes: {ex.Message}");
            }
        }

        private static void LoadCrafting(string modPath, string defaultModPath)
        {
            string file = GetFilePath(modPath, defaultModPath, "crafting.json");
            if (file == null) return;

            try
            {
                string json = File.ReadAllText(file);

                CraftingData crafting = JsonSerializer.Deserialize<CraftingData>(json);

                if (crafting == null)
                {
                    Debug.Error("[GameSetLoader] Crafting was null!");
                }

                if (crafting.Categories != null && crafting.Categories.Length > 0)
                {
                    foreach (var category in crafting.Categories)
                    {
                        if (!GameBlocks.CraftingCategories.ContainsKey(category.Id))
                        {

                            if(category.Name != "")
                            {
                                var item = GameBlocks.GetItemByName(category.Icon);

                                if(item != null)
                                {
                                    category.IconPtr = item.IconTextureId;
                                }
                            }
                            


                            GameBlocks.CraftingCategories.Add(category.Id, category);
                        }
                        else
                        {
                            Debug.Error("[GameSetLoader] Such a crafting category already exists: id - " + category.Id);
                        }

                    }


                    LoadBlueprints(crafting);
                    PutItemsToCategories();
                }

            }
            catch (Exception ex)
            {
                Debug.Error($"[GamesetLoader] Error loading blueprints: {ex.Message}");
            }
        }

        private static void PutItemsToCategories()
        {
            var items = GameBlocks.Item.Values.ToList();


            foreach (var item in items)
            {
                if (item == null) continue;

                var category = item.Category;

                if (!GameBlocks.CraftingCategories.ContainsKey(category)) continue;

                GUI.CraftingCategory.Data d = new GUI.CraftingCategory.Data();
                d.item = item;

                if (GameBlocks.Blueprints.ContainsKey(item.Id))
                {
                    d.blueprint = GameBlocks.Blueprints[item.Id];
                }

                GameBlocks.CraftingCategories[category].Items.Add(d);



            }
        }

        private static void LoadBlueprints(CraftingData data)
        {
            short id = 0;
            if (data == null) return;

            foreach (var e in data.Blueprints)
            {
                List<Ingredient> ingredient = new List<Ingredient>();
                Product product;
                Blueprint blueprint = new Blueprint();

                var item = GameBlocks.GetItemByName(e.Product.Item);

                if (item == null)
                {
                    Debug.Error($"[GameSetLoader] [Blueprints]: Product was not found - {e.Product.Item} . This Blueprint was skipped");
                    continue;
                }
                product = new Product(item, (byte)e.Product.Quantity);

                bool craftItself = false;
                foreach (var ing in e.Ingredients)
                {
                    var item2 = GameBlocks.GetItemByName(ing.Item);
                    if (item2 == null)
                    {
                        Debug.Error($"[GameSetLoader] [Blueprints]: Ingredient was not found - {ing.Item} . This Ingredient was skipped");
                        continue;
                    }

                    if(item2.Id == product.Item.Id)
                    {
                        craftItself = true;
                    }
                    ingredient.Add(new Ingredient(item2, (byte)ing.Quantity));
                }

                if (craftItself)
                {
                    Debug.Error($"[GameSetLoader] Blueprints: item <{item.Name}> needs itself for crafting! This blueprint was skipped");
                    continue;
                }

                blueprint.Ingredients = ingredient.ToArray();
                blueprint.Product = product;
                blueprint.Id = id++;


                var productID = product.Item.Id;
                if (!GameBlocks.Blueprints.ContainsKey(productID))
                {
                    GameBlocks.Blueprints.Add(productID, blueprint);
                    //Debug.Log("Blueprint loaded: product id " + productID);
                }


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
                Damage = data.Damage,
                Category = data.Category,
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
                Power = data.Power,
                Category = data.Category,
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

                HealAmount = data.HealAmount,
                PowerAmount = data.PowerAmount,
                UseSound = data.Sound,
                Category = data.Category,
            };
            GameBlocks.RegisterItem(consumableItem, data.Sprite);
        }

        private static void RegisterItem(ItemData data)
        {
            data.ValidateMaxStack();
            var item = new Item(
                (byte)data.MaxStack,
                data.Name,

                data.ModelDepth);
            item.Category = data.Category;
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
                        Lighting.AmbientColor = new Vector3(settings.AmbientColor.X / 255f, settings.AmbientColor.Y / 255f, settings.AmbientColor.Z / 255f);
                        Lighting.FogColor = new Vector3(settings.FogColor.X / 255f, settings.FogColor.Y / 255f, settings.FogColor.Z / 255f);
                        Lighting.FogDensity = settings.FogDensity;
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
            public List<DefaultItem> ItemsOnStart { get; set; } = new List<DefaultItem>();
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

        public class DefaultItem
        {
            public string Name { get; set; } = "unknown";
            public byte Count { get; set; } = 0;

        }

        public static void GiveStartItems(Astronaut player, Dictionary<short, Item> items)
        {
            if (player == null) return;

            if (ModInfo == null)
            {
                Debug.Error("No ModInfo found.");
                return;
            }

            if (items == null)
            {
                Debug.Error("No Items found.");
                return;
            }

            if (items.Count == 0) return;
            if (ModInfo.ItemsOnStart == null) return;
            if (ModInfo.ItemsOnStart.Count == 0) return;

            if(player.GameMode == GameMode.Spectator)
            {
                return;
            }

            foreach (var itemData in ModInfo.ItemsOnStart)
            {
                if (itemData.Count < 1) continue;
                if (itemData.Name == string.Empty) continue;

                Item item = null;

                foreach (var i in items)
                {
                    if (i.Value.Name.ToLower() == itemData.Name.ToLower())
                    {
                        item = i.Value;
                        break;
                    }
                }

                if (item != null)
                {
                    if (player.Panel.TryAddItem(item, itemData.Count))
                    {
                        Debug.Error($"Unknow error by adding a default item on start: {itemData.Name} [{itemData.Count}]");
                    }
                }
            }
        }



        private class ModSettings
        {
            public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        }
    }
}
