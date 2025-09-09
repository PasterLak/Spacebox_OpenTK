using Engine;
using Engine.Audio;
using Engine.Light;
using Engine.Utils;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Scenes;
using System.Text.Json;


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
                Debug.Error("[GameSetLoader] Mods directory created. Please add mods and try again.");
                return;
            }

            modPath = modId.ToLower() == defaultModId ? defaultModPath : FindModPath(modsDirectory, modId);

            if (string.IsNullOrEmpty(modPath))
            {
                Debug.Error($"[GameSetLoader] Mod with ID '{modId}' not found in Mods directory.");
                return;
            }

            string configFile = Path.Combine(modPath, "config.json");

            if (!File.Exists(configFile))
            {
                Debug.Error($"[GameSetLoader] Config file not found for mod '{modId}'.");
                return;
            }

            ModInfo = LoadConfig(configFile);
            if (ModInfo == null)
            {
                Debug.Error($"[GameSetLoader] Failed to load config for mod '{modId}'.");
                return;
            }

            LoadSounds(modPath);
            BlocksLoader.LoadBlocks(modPath, defaultModPath);
            LoadProjectiles(modPath, defaultModPath);
            LoadItems(modPath, defaultModPath);
            SetBlocksDrop();
            LoadRecipes(modPath, defaultModPath);
            LoadCrafting(modPath, defaultModPath);
            LoadLoot(modPath, defaultModPath);
            LoadSettings(modPath);
            LoadOptionalFiles(modPath);

            Debug.Success($"[GameSetLoader] Mod '{modId}' loaded successfully.");
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
                    
                    ModConfig config = JsonFixer.LoadJsonSafe < ModConfig >(configPath);
                    if (config != null && config.ModId.ToLower() == modId)
                    {
                        return directory;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Error($"[GameSetLoader] Error reading config from '{directory}': {ex.Message}");
                }
            }
            return string.Empty;
        }

        private static ModConfig LoadConfig(string configFile)
        {
            try
            {
             
                return JsonFixer.LoadJsonSafe<ModConfig>(configFile);
            
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Error loading config: {ex.Message}");
                return null;
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
                Debug.Error($"[GameSetLoader] Error loading sounds: {ex.Message}");
            }


        }

        public static string[] GetFilesRecursive(string rootPath, string[] extensionsWithDot)
        {
            var files = new List<string>();
            foreach (var ext in extensionsWithDot)
            {
                var ex = "*" + ext;
                var pattern = ex.StartsWith("*.") ? ex : $"*.{ex.TrimStart('.')}";
                var allFiles = Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories);

                var filteredFiles = allFiles.Where(file =>
                {
                    var directory = Path.GetDirectoryName(file);
                    var pathParts = directory.Replace(rootPath, "").Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                    return !pathParts.Any(part => part.StartsWith("_"));
                });

                files.AddRange(filteredFiles);
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

                

        public static string ValidateIdString(string @namespace, string idString)
        {
            if (string.IsNullOrEmpty(idString))
            {
                Debug.Error("ID cannot be null or empty, using fallback 'unknown_item'");
                idString = "unknown_item";
            }

            string cleanId = "";
            for (int i = 0; i < idString.Length; i++)
            {
                char c = idString[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')
                {
                    cleanId += c;
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    cleanId += char.ToLower(c);
                }
                else if (c == ' ' || c == '-')
                {
                    cleanId += '_';
                }
            }

            if (cleanId != idString)
            {
                Debug.Error($"ID '{idString}' contained invalid characters, cleaned to '{cleanId}'");
                idString = cleanId;
            }

            if (string.IsNullOrEmpty(idString))
            {
                Debug.Error("ID became empty after cleaning, using fallback 'item'");
                idString = "item";
            }

            if (char.IsDigit(idString[0]) || idString[0] == '_')
            {
                Debug.Error($"ID '{idString}' starts with invalid character, adding 'item_' prefix");
                idString = "item_" + idString;
            }

            while (idString.EndsWith("_"))
            {
                idString = idString.Substring(0, idString.Length - 1);
                Debug.Error($"ID ended with underscore, trimmed to '{idString}'");
            }

            while (idString.Contains("__"))
            {
                idString = idString.Replace("__", "_");
                Debug.Error($"ID contained double underscores, fixed to '{idString}'");
            }

            if (idString.Length < 2)
            {
                Debug.Error($"ID '{idString}' too short, using 'item'");
                idString = "item";
            }

            if (idString.Length > 64)
            {
                idString = idString.Substring(0, 64);
                Debug.Error($"ID too long, truncated to '{idString}'");
            }

            string originalId = idString;
            string fullId = $"{@namespace}:{idString}";
            int counter = 1;

            while (GameAssets.HasItem(fullId))
            {
                idString = $"{originalId}_{counter}";
                fullId = $"{@namespace}:{idString}";
                counter++;

                if (counter == 1000)
                {
                    Debug.Error($"Too many duplicate IDs for '{originalId}', using random suffix");
                    idString = $"{originalId}_{System.DateTime.Now.Ticks % 10000}";
                    break;
                }
            }

            if (idString != originalId)
            {
                Debug.Error($"ID '{@namespace}:{originalId}' already exists, using '{@namespace}:{idString}' instead");
            }

            return idString;
        }

        public static string CombineId(string modId, string itemId)
        {
            return $"{modId}:{itemId}";
        }

        public static (string modId, string itemId) SplitId(string fullId)
        {
            int colonIndex = fullId.IndexOf(':');
            if (colonIndex == -1)
            {
                Debug.Error($"Invalid ID format: '{fullId}'. Expected format 'modId:itemId'");
                return ("unknown", fullId);
            }

            string modId = fullId.Substring(0, colonIndex);
            string itemId = fullId.Substring(colonIndex + 1);

            return (modId, itemId);
        }

        

        private static void LoadProjectiles(string modPath, string defaultModPath)
        {
            string projectilesFile = GetFilePath(modPath, defaultModPath, "projectiles.json");
            if (projectilesFile == null) return;

            try
            {
                
         
                List<ProjectileJSON> projectiles = JsonFixer.LoadJsonSafe<List<ProjectileJSON>>(projectilesFile);

                short id = 0;

                foreach (var proj in projectiles)
                {
                    proj.ID = proj.ID.ToLower();

                    if (GameAssets.Projectiles.ContainsKey(id))
                    {
                        Debug.Error($"[GamesetLoader] Error loading projectiles: projectile with the name {proj.ID} is already registered and was skpped! Use a different name!");
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
                    string idString = "";
                    Color3Byte color = new Color3Byte();
                    if (itemElement.TryGetProperty("Type", out JsonElement typeElement))
                    {
                        type = typeElement.GetString().ToLower();
                    }

                    try
                    {
                        if (itemElement.TryGetProperty("GlowColor", out JsonElement t))
                        {
                            color = t.Deserialize<Color3Byte>();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Error($"[GameSetLoader] Item GlowColor: " + ex);
                        color = Color3Byte.Zero;
                    }
                    

                    if (itemElement.TryGetProperty("ID", out JsonElement idElement))
                    {
                        idString = idElement.GetString();
                        idString = ValidateIdString(ModInfo.ModId, idString);
                        idString = CombineId(ModInfo.ModId, idString);
                        if (GameAssets.HasItem(idString))
                        {
                            Debug.Error($"[GameSetLoader] Item with ID '{idString}' already exists. Skipping duplicate.");
                            continue;
                        }
                    }
                    else
                    {
                        Debug.Error($"[GameSetLoader] Item is missing an ID. Skipping item.");
                        continue;
                    }


                    Item registeredItem = null;
                    switch (type)
                    {
                        case "weapon":
                            var weaponData = itemElement.Deserialize<WeaponItemData>();
                            if (weaponData == null) continue;
                            registeredItem = RegisterWeaponItem(weaponData, idString);
                            break;

                        case "drill":
                            var drillData = itemElement.Deserialize<DrillItemData>();
                            if (drillData == null) continue;
                            registeredItem = RegisterDrillItem(drillData, idString);
                            break;

                        case "consumable":
                            var consumableData = itemElement.Deserialize<ConsumableItemData>();
                            if (consumableData == null) continue;
                            registeredItem = RegisterConsumableItem(consumableData, idString);
                            break;

                        case "item":
                        default:
                            var itemData = itemElement.Deserialize<ItemData>();
                            if (itemData == null) continue;
                            registeredItem = RegisterItem(itemData, idString);
                            break;
                    }

                    if (registeredItem != null)
                    {
                        if (itemElement.TryGetProperty("Description", out var v))
                        {
                            registeredItem.Description = v.GetString();
                        }

                        registeredItem.Color = color;
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

        private static void SetBlocksDrop()
        {
            foreach (var block in GameAssets.Blocks.Values)
            {
                if(block == null) continue;
                if (block.Id == 0) continue;

                var drop = block.Drop;
                var dropIdStr = drop.Item.Id_string;

                if (dropIdStr == "$self")
                {
                     block.Drop.Item = block.AsItem;
                   
                }
                else
                {

                    var fullID = CombineId(ModInfo.ModId, dropIdStr);

                    if(fullID == block.Id_string)
                    {
                        block.Drop.Item = block.AsItem;
                                         
                        continue;
                    }

                    if (GameAssets.TryGetItemByFullID(fullID, out var item))
                    {
                        block.Drop.Item = item;
                       
                    }
                    else
                    {
                        block.Drop.Item = block.AsItem;
                       
                        Debug.Error($"[GameSetLoader] Block <{block.Name}> has a wrong drop item! - {dropIdStr}. Selected itself as a drop");
                    }

                }
            }
        }

        private static void LoadRecipes(string modPath, string defaultModPath)
        {
            string itemsFile = GetFilePath(modPath, defaultModPath, "recipes.json");
            if (itemsFile == null) return;

            try
            {
               
                RecipesJSON t = JsonFixer.LoadJsonSafe<RecipesJSON>(itemsFile);


                foreach (var typ in t.RecipeCatalog)
                {
                    var type = typ.Type;

                    foreach (var r in typ.Recipes)
                    {
                        if (r == null) continue;

                        var ingredientFullId = CombineId(ModInfo.ModId, r.Ingredient.Item);
                        var productFullId = CombineId(ModInfo.ModId, r.Product.Item);

                        var item = GameAssets.GetItemByFullID(ingredientFullId);
                        var item2 = GameAssets.GetItemByFullID(productFullId);

                        if (item == null)
                        {
                            Debug.Error($"[GameSetLoader] Recipes: ingredient was not found - {r.Ingredient.Item} . This recipe was skipped");
                            continue;
                        }
                        if (item2 == null)
                        {
                            Debug.Error($"[GameSetLoader] Recipes: product was not found - {r.Product.Item} . This recipe was skipped");
                            continue;
                        }

                        if (item == item2)
                        {
                            Debug.Error($"[GameSetLoader] Recipes: item <{item.Name}> needs itself for crafting! This recipe was skipped");
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
         
                CraftingData crafting = JsonFixer.LoadJsonSafe<CraftingData>(file);

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
                                var item = GameAssets.GetItemByFullID(CombineId(ModInfo.ModId, category.Icon));

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

                var productFullID = CombineId(ModInfo.ModId, e.Product.Item);
                item = GameAssets.GetItemByFullID(productFullID);

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
                        var ingredientFullID = CombineId(ModInfo.ModId, ing.Item);
                        item2 = GameAssets.GetItemByFullID(ingredientFullID);
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
                }


            }

        }

        private static Item RegisterWeaponItem(WeaponItemData data, string id)
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
            weaponItem.Id_string = id;
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



        private static Item RegisterDrillItem(DrillItemData data, string id)
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
                Id_string = id,
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
            eraser.Id_string = "default:eraser_tool";
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
            eraser.Id_string = "default:camera_point";
            eraser.Description = "LMB - remove last point\nMMB - remove all points\nRMB - add point\nEnter - start\nAlt+Scroll - set speed";
            GameAssetsRegister.RegisterItem(eraser, "cameraPoint");
        }


        private static Item RegisterConsumableItem(ConsumableItemData data, string id)
        {
            data.ValidateMaxStack();
            var consumableItem = new ConsumableItem(
                (byte)data.MaxStack,
                data.Name,

                data.ModelDepth)
            {
                Id_string = id,
                HealAmount = data.HealAmount,
                PowerAmount = data.PowerAmount,
                UseSound = data.Sound,
                Category = data.Category,
            };
            GameAssetsRegister.RegisterItem(consumableItem, data.Sprite);

            return consumableItem;
        }

        private static Item RegisterItem(ItemData data, string id)
        {
            data.ValidateMaxStack();
            var item = new Item(
                (byte)data.MaxStack,
                data.Name,
                
                data.ModelDepth);
            item.Category = data.Category;
            item.Id_string = id;
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
                  
                    ModSettings settings = JsonFixer.LoadJsonSafe<ModSettings>(settingsFile);

                }
                catch (Exception ex)
                {
                    Debug.Error($"[GameSetLoader] Error loading settings: {ex.Message}");
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
                   
                    Modlighting settings = JsonFixer.LoadJsonSafe<Modlighting>(file);

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

        public static string GetFilePath(string modPath, string defaultModPath, string fileName)
        {
            string filePath = Path.Combine(modPath, fileName);
            string defaultFilePath = Path.Combine(defaultModPath, fileName);

            if (!File.Exists(filePath))
            {
                filePath = defaultFilePath;
                Debug.Error($"[GameSetLoader] {fileName} not found in mod. Using default.");
                if (!File.Exists(filePath))
                {
                    Debug.Error($"[GameSetLoader] Default {fileName} not found.");
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
            public string ID { get; set; } = "unknown";
            public byte Count { get; set; } = 0;

        }

        public static void GiveStartItems(Astronaut player, Dictionary<string, Item> allGameItems)
        {
            if (player?.GameMode == GameMode.Spectator ||
                ModInfo?.ItemsOnStart?.Count == 0 ||
                allGameItems?.Count == 0)
                return;

            foreach (var itemData in ModInfo.ItemsOnStart)
            {
                if (itemData.Count < 1 || string.IsNullOrEmpty(itemData.ID))
                    continue;

                if(ModInfo.ModId == "") Debug.Error("[GameSetLoader][GiveStartItems] ModId is empty! Cannot give start items!");

                string fullId = CombineId(ModInfo.ModId, itemData.ID);

                if (GameAssets.TryGetItemByFullID(fullId, out Item item))
                {
                    if (!player.Panel.TryAddItem(item, itemData.Count))
                    {
                        Debug.Error($"[GameSetLoader][GiveStartItems] Failed to add start item: {itemData.ID} [{itemData.Count}]");
                    }
                }
                else
                {
                    Debug.Error($"[GameSetLoader][GiveStartItems] Start item not found: {fullId}");
                }
            }
        }



        private class ModSettings
        {
            public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        }
    }
}
