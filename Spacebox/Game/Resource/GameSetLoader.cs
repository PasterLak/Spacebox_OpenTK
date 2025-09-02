using System.Text.Json;

using Engine.Audio;
using Spacebox.Game.Player;
using Engine;
using Spacebox.Scenes;
using Engine.Light;
using OpenTK.Mathematics;
using Spacebox.Game.Player.GameModes;


namespace Spacebox.Game.Resource
{
    public static class GameSetLoader
    {
        public static ModConfig ModInfo;
        private static string modPath;
        public static void Load(string modId, bool isMultiplayer, string serverName)
        {
            string modsDirectory = ModPath.GetModsPath(isMultiplayer, serverName);
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
            LoadProjectiles(modPath, defaultModPath);
            LoadItems(modPath, defaultModPath);
            LoadRecipes(modPath, defaultModPath);
            LoadCrafting(modPath, defaultModPath);
            LoadLoot(modPath, defaultModPath);
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
                        GameAssets.DustTexture = loadedTexture;
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
                string[] files = GetFilesRecursive(audioPath, new string[] { ".wav", ".ogg" });
                foreach (string file in files)
                {

                    ProcessSoundFile(file);
                }

                if (!GameAssets.Sounds.ContainsKey("error"))
                    GameAssets.Sounds.Add("error", Resources.Load<AudioClip>("error"));
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading sounds: {ex.Message}");
            }


        }

        public static string[] GetFilesRecursive(string rootPath, string[] extensionsWithDot)
        {

            var files = new List<string>();

            foreach (var ext in extensionsWithDot)
            {
                var ex = "*" + ext;
                var pattern = ex.StartsWith("*.") ? ex : $"*.{ex.TrimStart('.')}";
                files.AddRange(Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories));
            }

            return files.ToArray();
        }


        private static void ProcessSoundFile(string file)
        {
            var soundName = Path.GetFileNameWithoutExtension(file);

            if (!GameAssets.Sounds.ContainsKey(soundName))
            {
                var clip = Resources.Load<AudioClip>(file);
                GameAssets.Sounds.Add(soundName, clip);
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


                GameAssetsRegister.RegisterBlock(air);

                foreach (var block in blocks)
                {
                    block.Type = block.Type.ToLower();
                    block.Sides = block.Sides.ToLower();
                    block.Top = block.Top.ToLower();
                    block.Bottom = block.Bottom.ToLower();

                    if (!BlockFactory.ValidateBlockType(block.Type))
                    {
                        Debug.Error($"Block '{block.Name}' has an invalid type and was skipped");
                        Debug.Error($"Valid types are: {string.Join(", ", BlockFactory.GetBlockTypes())}");
                        continue;
                    }

                    bool sameSides = block.Top == "" || block.Top == "" || block.Sides == block.Top && block.Sides == block.Top;
                    bool hasLightColor = block.LightColor != Color3Byte.Black;
                    var blockColor = hasLightColor ? block.LightColor.ToVector3() : Color3Byte.Black.ToVector3();

                    BlockData blockData = new BlockData(block.Name, block.Type, new Vector2Byte(0, 0), block.IsTransparent, blockColor)
                    {
                        AllSidesAreSame = sameSides,
                        TopUVIndex = new Vector2Byte(),
                        BottomUVIndex = new Vector2Byte(),

                    };

                    blockData.Description = block.Description;

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
                        blockData.Durability = (byte)block.Durability;
                    }
                    else
                    {
                        blockData.Durability = byte.MaxValue;
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

                    GameAssetsRegister.RegisterBlock(blockData);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[GamesetLoader] Error loading blocks: {ex.Message}");
            }
        }

        private static void GiveBlockSounds(BlockData blockData, ModBlockData modBlockData)
        {
            if (!GameAssets.Sounds.ContainsKey(modBlockData.SoundPlace))
            {
                blockData.SetDefaultPlaceSound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> has a wrong place sound! - {modBlockData.SoundPlace}. Selected a default one");
            }
            else
            {
                blockData.SoundPlace = modBlockData.SoundPlace;
            }
            if (!GameAssets.Sounds.ContainsKey(modBlockData.SoundDestroy))
            {
                blockData.SetDefaultDestroySound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> has a wrong destroy sound! - {modBlockData.SoundDestroy}. Selected a default one");
            }
            else
            {
                blockData.SoundDestroy = modBlockData.SoundDestroy;
            }
        }



        private static void LoadProjectiles(string modPath, string defaultModPath)
        {
            string projectilesFile = GetFilePath(modPath, defaultModPath, "projectiles.json");
            if (projectilesFile == null) return;

            try
            {
                string json = File.ReadAllText(projectilesFile);


                List<ProjectileJSON> projectiles = JsonSerializer.Deserialize<List<ProjectileJSON>>(json);

                short id = 0;

                foreach (var proj in projectiles)
                {
                    proj.Name = proj.Name.ToLower();

                    if (GameAssets.Projectiles.ContainsKey(id))
                    {
                        Debug.Error($"[GamesetLoader] Error loading projectiles: projectile with the name {proj.Name} is already registered and was skpped! Use a different name!");
                        continue;
                    }

                    if (proj.Damage > byte.MaxValue) proj.Damage = byte.MaxValue;
                    if (proj.DamageBlocks > byte.MaxValue) proj.DamageBlocks = byte.MaxValue;
                    if (proj.Mass > byte.MaxValue) proj.Mass = byte.MaxValue;

                    var newProj = new ProjectileParameters(id++, proj);

                    GameAssets.Projectiles.Add(newProj.ID, newProj);

                }

                GameAssets.Projectiles.Add(short.MaxValue, ProjectileParameters.GetErrorProjectile());


            }
            catch (Exception ex)
            {
                Debug.Error($"[GamesetLoader] Error loading projectiles: {ex.Message}");
            }
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
                    Item registeredItem = null;
                    switch (type)
                    {
                        case "weapon":
                            var weaponData = itemElement.Deserialize<WeaponItemData>();
                            if (weaponData == null) continue;
                            registeredItem = RegisterWeaponItem(weaponData);
                            break;

                        case "drill":
                            var drillData = itemElement.Deserialize<DrillItemData>();
                            if (drillData == null) continue;
                            registeredItem = RegisterDrillItem(drillData);
                            break;

                        case "consumable":
                            var consumableData = itemElement.Deserialize<ConsumableItemData>();
                            if (consumableData == null) continue;
                            registeredItem = RegisterConsumableItem(consumableData);
                            break;

                        case "item":
                        default:
                            var itemData = itemElement.Deserialize<ItemData>();
                            if (itemData == null) continue;
                            registeredItem = RegisterItem(itemData);
                            break;
                    }

                    if (registeredItem != null)
                    {
                        if (itemElement.TryGetProperty("Description", out var v))
                        {
                            registeredItem.Description = v.GetString();
                        }
                    }
                }

                RegisterEraserItem();
                RegisterEraserItem2();
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

                RecipesJSON t = JsonSerializer.Deserialize<RecipesJSON>(json);


                foreach (var typ in t.RecipeCatalog)
                {
                    var type = typ.Type;

                    foreach (var r in typ.Recipes)
                    {
                        if (r == null) continue;

                        var item = GameAssets.GetItemByName(r.Ingredient.Item);
                        var item2 = GameAssets.GetItemByName(r.Product.Item);

                        if (item == null)
                        {
                            Debug.Error($"[GameSetLoader] Recipes: ingredient was not found - {r.Ingredient.Item} . This recipe was skipped");
                            continue;
                        }
                        if (item2 == null) continue;

                        if (item.Id == item2.Id)
                        {
                            Debug.Error($"[GameSetLoader] Recipes: product was not found - {r.Ingredient.Item} . This recipe was skipped");
                            continue;
                        }

                        r.Type = type;

                        GameAssetsRegister.RegisterRecipe(r, item, item2);
                    }
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
                        if (!GameAssets.CraftingCategories.ContainsKey(category.Id))
                        {

                            if (category.Name != "")
                            {
                                var item = GameAssets.GetItemByName(category.Icon);

                                if (item != null)
                                {
                                    category.IconPtr = item.IconTextureId;
                                }
                            }


                            GameAssets.CraftingCategories.Add(category.Id, category);
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

        private static void LoadLoot(string modPath, string defaultModPath)
        {
            string file = GetFilePath(modPath, defaultModPath, "loot.json");
            if (file == null) return;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                string json = File.ReadAllText(file);
                LootConfig loot = JsonSerializer.Deserialize<LootConfig>(json, options);

                if (loot == null)
                {
                    Debug.Error("[GameSetLoader] Loot config was null!");
                    return;
                }

                loot.Validate();
                GameAssets.LootConfig = loot;

            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Error loading loot config: {ex.Message}");
                return;
            }
        }


        private static void PutItemsToCategories()
        {
            var items = GameAssets.Items.Values.ToList();


            foreach (var item in items)
            {
                if (item == null) continue;

                var category = item.Category;

                if (!GameAssets.CraftingCategories.ContainsKey(category)) continue;

                GUI.CraftingCategory.Data d = new GUI.CraftingCategory.Data();
                d.item = item;

                if (GameAssets.Blueprints.ContainsKey(item.Id))
                {
                    d.blueprint = GameAssets.Blueprints[item.Id];
                }

                GameAssets.CraftingCategories[category].Items.Add(d);



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

                Item? item = null;

                item = GameAssets.GetItemByName(e.Product.Item);

                if (item == null)
                {
                    Debug.Error($"[GameSetLoader] [Blueprints]: Product was not found - {e.Product.Item} . This Blueprint was skipped");
                    continue;

                }
                product = new Product(item, (byte)e.Product.Quantity);

                bool craftItself = false;
                foreach (var ing in e.Ingredients)
                {
                    Item? item2 = null;

                    if (ing.Item.ToLower() == "$health".ToLower())
                    {
                        item2 = new Item(255, "$health", 0.5f);
                        item2.IconTextureId = IntPtr.Zero;
                       
                    }
                    else
                    {
                        item2 = GameAssets.GetItemByName(ing.Item);
                    }

                    if (item2 == null)
                    {
                        Debug.Error($"[GameSetLoader] [Blueprints]: Ingredient was not found - {ing.Item} . This Ingredient was skipped");
                        continue;
                    }

                    if (item2.Id == product.Item.Id)
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
                if (!GameAssets.Blueprints.ContainsKey(productID))
                {
                    GameAssets.Blueprints.Add(productID, blueprint);
                    //Debug.Log("Blueprint loaded: product id " + productID);
                }


            }

        }

        private static Item RegisterWeaponItem(WeaponItemData data)
        {
            data.MaxStack = 1;

            short projectileID = 0;
            data.Projectile = data.Projectile.ToLower();
            if (GameAssets.TryGetProjectileByName(data.Projectile, out var projectile))
            {
                projectileID = projectile.ID;
            }
            else
            {
                Debug.Error($"[GameSetLoader] RegisterWeaponItem: projectile with name <{data.Projectile}> for weapone <{data.Name}>  was not found!");
                projectileID = short.MaxValue;
            }

            var weaponItem = new WeaponItem(
                (byte)data.MaxStack,
                data.Name,

                data.ModelDepth)
            {
                Category = data.Category,
            };
            weaponItem.ProjectileID = projectileID;
            weaponItem.ReloadTime = data.ReloadTime;
            weaponItem.AnimationSpeed = data.AnimationSpeed;
            weaponItem.Pushback = data.Pushback;
            weaponItem.Spread = data.Spread;

            if (GameAssets.Sounds.ContainsKey(data.ShotSound))
            {
                weaponItem.ShotSound = data.ShotSound;
            }
            else
            {
                Debug.Error($"[GameSetLoader] RegisterWeaponItem: ShotSound with name <{data.ShotSound}> for weapone <{data.Name}>  was not found!");
                weaponItem.ShotSound = "error";
            }

            weaponItem.PowerUsage = data.PowerUsage;

            GameAssetsRegister.RegisterItem(weaponItem, data.Sprite);

            return weaponItem;
        }



        private static Item RegisterDrillItem(DrillItemData data)
        {
            data.MaxStack = 1;
            var drillItem = new DrillItem(
                (byte)data.MaxStack,
                data.Name,

                data.ModelDepth)
            {
                Power = data.Power,
                PowerUsage = (byte)data.PowerUsage,
                Category = data.Category,

                DrillColor = data.DrillColor,
            };
            GameAssetsRegister.RegisterItem(drillItem, data.Sprite);

            return drillItem;
        }

        private static void RegisterEraserItem()
        {

            var eraser = new EraserToolItem(
               "Eraser Tool",
               2f)
            {

            };
            eraser.Description = "LMB - select block 1\nRMB - select block 2\nMMB - reset\nEnter - confirm";
            GameAssetsRegister.RegisterItem(eraser, "eraser");
        }
        private static void RegisterEraserItem2()
        {

            var eraser = new CameraPointItem(
               "Camera Point",
               2f)
            {

            };
            eraser.Description = "LMB - remove last point\nMMB - remove all points\nRMB - add point\nEnter - start\nAlt+Scroll - set speed";
            GameAssetsRegister.RegisterItem(eraser, "cameraPoint");
        }


        private static Item RegisterConsumableItem(ConsumableItemData data)
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
            GameAssetsRegister.RegisterItem(consumableItem, data.Sprite);

            return consumableItem;
        }

        private static Item RegisterItem(ItemData data)
        {
            data.ValidateMaxStack();
            var item = new Item(
                (byte)data.MaxStack,
                data.Name,

                data.ModelDepth);
            item.Category = data.Category;
            GameAssetsRegister.RegisterItem(item, data.Sprite);

            return item;
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
                        Lighting.AmbientColor = settings.AmbientColor.ToVector3();
                        Lighting.FogColor = settings.FogColor.ToVector3();
                        Lighting.FogDensity = settings.FogDensity;
                    }
                    else Debug.Error($"[GameSetLoader] Error loading lighting settings: Modlighting settings = null");

                    Debug.Log("[GameSetLoader] Lighting file was loaded! ");
                }
                catch (Exception ex)
                {
                    Debug.Error($"[GameSetLoader] Error loading lighting settings: {ex.Message}");
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
            public Color3Byte AmbientColor { get; set; } = Color3Byte.White;
            public Color3Byte FogColor { get; set; } = Color3Byte.Black;
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

            if (player.GameMode == GameMode.Spectator)
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

                    }
                    else
                    {
                        Debug.Error($"[GameSetLoader][GiveStartItems] Unknow error by adding a default item on start: {itemData.Name} [{itemData.Count}]");
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
