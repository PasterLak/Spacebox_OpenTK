using Engine.Graphics.Atlas;
using Engine;
using Engine.Audio;
using Engine.Light;
using Engine.Utils;
using Spacebox.Game.Disassembly;
using Spacebox.Game.Ships;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Scenes;
using System.Text.Json;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource;

public static class GameSetLoader
{
    public static ModConfig ModInfo;
    public static GameSetStack Stack { get; private set; }
    public static GameSetContent Content { get; private set; }
    private static string modPath;

    private static readonly Queue<Action> _postLoadActions = new Queue<Action>();

    public static void EnqueuePostLoadAction(Action action)
    {
        _postLoadActions.Enqueue(action);
    }

    private static void ExecutePostLoadActions()
    {
        while (_postLoadActions.Count > 0)
        {
            _postLoadActions.Dequeue()?.Invoke();
        }

    }

    public static void Unload()
    {
        if (ModInfo != null)
        {
            ModInfo.Dispose();
        }
        ModInfo = null;
        modPath = null;
        Stack = null;
        Scopes = null;
        Content = null;
        Spacebox.Game.Tutorial.TutorialLibrary.Clear();
    }
    public static void Load(GameSetStack stack, GameSetAssetIndex assets)
    {
        Unload();

        if (stack == null)
        {
            Debug.Error("[GameSetLoader] No GameSet to load.");
            return;
        }

        modPath = stack.Base.Folder;
        string defaultModPath = stack.Fallback?.Root ?? modPath;
        string configFile = Path.Combine(modPath, "config.json");

        if (!File.Exists(configFile))
        {
            Debug.Error($"[GameSetLoader] Config file not found for mod '{stack.Base.Id}'.");
            return;
        }

        ModInfo = LoadConfig(configFile);
        if (ModInfo == null)
        {
            Debug.Error($"[GameSetLoader] Failed to load config for mod '{stack.Base.Id}'.");
            return;
        }
        ModInfo.ModPath = modPath;
        ModInfo.FolderName = stack.Base.FolderName;
        var files = GameSetFiles.For(stack);
        ApplyExtensionSettings(stack);
        var spawnParameters = Spacebox.Game.Player.Respawn.SpawnParameters.FromFile(files.File(Spacebox.Game.Player.Respawn.SpawnParameters.RelativePath));
        var extensionConfigs = new List<ModConfig>();
        foreach (var extension in stack.Extensions) extensionConfigs.Add(extension.Config);
        ModInfo.ItemsOnStart = Spacebox.Game.Player.Respawn.SpawnParameters.StartItems(spawnParameters, ModInfo, extensionConfigs);

        Stack = stack;
        Scopes = IdScopes.For(stack);
        Content = stack.ReadContent();
        foreach (var warning in Content.Warnings) Debug.Warning($"[GameSetLoader] {warning}");

        LoadSounds(assets);
        LoadProjectiles(files);
        BlocksLoader.LoadBlocks(Content, stack.SearchRootsNewestFirst());
        LoadItems(Content);
        SetBlocksDrop();
        LoadRecipes(Content);
        LoadCrafting(Content);
        DisassemblyRecipes.Initialize(DisassemblySettingsLoader.Load(files, ModInfo.ModId));
        ShipPhysicsSettings.Current = ShipPhysicsSettingsLoader.Load(files);
        LoadLoot(files);
        MobsLoader.Load(files);
        ResolveSpawnerMobs();
        GameModesLoader.Load(files);
        MobIconRenderer.ApplyToSpawners();
        Spacebox.Game.Tutorial.TutorialLoader.Load(files, ModInfo.ModId);
        LoadSettings(modPath);
        LoadLighting(files.File(LightingFile));

        var generator = GenerationLoader.Load(files);
        foreach (var warning in files.Warnings) Debug.Warning($"[GameSetLoader] {warning}");
        generator.PlayerSpawn = Spacebox.Game.Player.Respawn.SpawnParameters.Rules(spawnParameters, generator.PlayerSpawn);
        Generation.World.WorldGenerator = generator;

        Theme.LoadThemeColors(ModInfo.ModPath);

        ExecutePostLoadActions();

        string extensions = stack.Extensions.Count == 0 ? "" : $" with extensions {string.Join(", ", stack.ExtensionIds)}";
        Debug.Success($"[GameSetLoader] Mod '{stack.Base.Id}'{extensions} loaded successfully.");

        //generator.DebugPrint();
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

    private static void LoadSounds(GameSetAssetIndex assets)
    {
        try
        {
            foreach (var pair in assets.Sounds)
            {
                if (!GameAssets.Sounds.ContainsKey(pair.Key)) GameAssets.Sounds.Add(pair.Key, Resources.Load<AudioClip>(pair.Value));
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
        return itemId != null && itemId.Contains(':') ? itemId : $"{modId}:{itemId}";
    }

    public static IdScopes Scopes { get; private set; }

    public static string ResolveName(string name, string ns, Func<string, bool> exists, IEnumerable<string> allNames = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        string trimmed = name.Trim().ToLowerInvariant();
        if (exists(trimmed)) return trimmed;
        if (Scopes != null)
        {
            string resolved = Scopes.Resolve(Scopes.Normalize(trimmed), ns, exists);
            if (exists(resolved)) return resolved;
        }
        return trimmed.Contains(':') || allNames == null ? trimmed : UniqueNamespaced(trimmed, allNames) ?? trimmed;
    }

    private static string UniqueNamespaced(string name, IEnumerable<string> allNames)
    {
        string found = null;
        foreach (var candidate in allNames)
        {
            if (!candidate.EndsWith(":" + name, StringComparison.OrdinalIgnoreCase)) continue;
            if (found != null) return null;
            found = candidate;
        }
        return found;
    }

    public static string NamespaceOf(string fullId)
    {
        int colon = fullId?.IndexOf(':') ?? -1;
        return colon < 0 ? null : fullId.Substring(0, colon);
    }

    private static void ResolveSpawnerMobs()
    {
        foreach (var item in GameAssets.Items.Values)
        {
            if (item is MobSpawnerItem spawner)
                spawner.MobId = ResolveName(spawner.MobId, NamespaceOf(spawner.Id_string), GameAssets.Mobs.ContainsKey);
        }
    }

    public static (string ns, string shortId) DeclaredId(string rawId, string entryNamespace)
    {
        string baseNamespace = ModInfo?.ModId ?? "";
        string id = rawId ?? "";
        int colon = id.IndexOf(':');
        if (colon >= 0) return (id.Substring(0, colon).ToLowerInvariant(), id.Substring(colon + 1));
        return (string.IsNullOrEmpty(entryNamespace) ? baseNamespace : entryNamespace, id);
    }

    public static string ResolveId(string itemId, string ns)
    {
        if (string.IsNullOrEmpty(itemId) || itemId.Contains(':')) return itemId;
        string baseNamespace = ModInfo?.ModId ?? "";
        if (Scopes != null)
        {
            foreach (var space in Scopes.Search(ns))
            {
                string candidate = CombineId(space, itemId);
                if (GameAssets.HasItem(candidate)) return candidate;
            }
        }
        return CombineId(baseNamespace, itemId);
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



    private static void LoadProjectiles(GameSetFiles gameSetFiles)
    {
        short internalIdCounter = (short)GameAssets.Projectiles.Count;

        try
        {
            foreach (var file in gameSetFiles.Folder("Projectiles"))
            {
                try
                {
                    ProjectileJSON proj = GameSetFiles.Deserialize<ProjectileJSON>(file.Data);

                    if (proj == null) continue;

                    proj.ID = file.Name.ToLower();

                    if (GameAssets.Projectiles.ContainsKey(internalIdCounter))
                    {
                        Debug.Error($"[GamesetLoader] Projectile '{proj.ID}' is already registered! Skipping.");
                        continue;
                    }

                    if (proj.Damage > byte.MaxValue) proj.Damage = byte.MaxValue;
                    if (proj.DamageBlocks > byte.MaxValue) proj.DamageBlocks = byte.MaxValue;
                    if (proj.Penetration > byte.MaxValue) proj.Penetration = byte.MaxValue;
                    if (proj.PossibleRicochets < 0) proj.PossibleRicochets = 0;
                    if (proj.PossibleRicochets > byte.MaxValue) proj.PossibleRicochets = byte.MaxValue;
                    if (proj.RicochetAngle < 0) proj.RicochetAngle = 0;


                    var newProj = new ProjectileParameters(internalIdCounter++, proj);

                    GameAssets.Projectiles.Add(newProj.ID, newProj);
                }
                catch (Exception ex)
                {
                    Debug.Error($"[GamesetLoader] Error loading projectile file '{file.Source}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[GamesetLoader] Critical error in LoadProjectiles: {ex.Message}");
        }
    }

    private static void LoadItems(GameSetContent content)
    {
        try
        {
            foreach (var entry in content.Items)
            {
                JsonElement itemElement = JsonSerializer.SerializeToElement(entry.Data);

                try
                {
                    string type = "item";
                    string idString = "";
                    Color3Byte color = new Color3Byte();

                    if (ContentJson.TryGetProperty(itemElement, "Type", out JsonElement typeElement))
                    {
                        type = typeElement.GetString().ToLower();
                    }

                    if (ContentJson.TryGetProperty(itemElement, "GlowColor", out JsonElement t))
                    {
                        try { color = t.Deserialize<Color3Byte>(); } catch { color = Color3Byte.Zero; }
                    }

                    if (ContentJson.TryGetProperty(itemElement, "ID", out JsonElement idElement))
                    {
                        var (itemNamespace, shortId) = DeclaredId(idElement.GetString(), entry.Namespace);
                        idString = ValidateIdString(itemNamespace, shortId);
                        idString = CombineId(itemNamespace, idString);
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
                            var weaponData = itemElement.Deserialize<WeaponItemJSON>(ContentJson.CaseInsensitive);
                            if (weaponData != null) registeredItem = RegisterWeaponItem(weaponData, idString);
                            break;
                        case "drill":
                            var drillData = itemElement.Deserialize<DrillItemJSON>(ContentJson.CaseInsensitive);
                            if (drillData != null) registeredItem = RegisterDrillItem(drillData, idString);
                            break;
                        case "consumable":
                            var consumableData = itemElement.Deserialize<ConsumableItemJSON>(ContentJson.CaseInsensitive);
                            if (consumableData != null) registeredItem = RegisterConsumableItem(consumableData, idString);
                            break;
                        case "repair_tool":
                            var repairData = itemElement.Deserialize<RepairToolItemJSON>(ContentJson.CaseInsensitive);
                            if (repairData != null) registeredItem = RegisterRepairToolItem(repairData, idString);
                            break;
                        case "mob_spawner":
                            var spawnerData = itemElement.Deserialize<MobSpawnerItemJSON>(ContentJson.CaseInsensitive);
                            if (spawnerData != null) registeredItem = RegisterMobSpawnerItem(spawnerData, idString);
                            break;
                        case "picture":
                            var pictureData = itemElement.Deserialize<PictureItemJSON>(ContentJson.CaseInsensitive);
                            if (pictureData != null) registeredItem = RegisterPictureItem(pictureData, idString, entry);
                            break;
                        case "equipment":
                            var equipmentData = itemElement.Deserialize<EquipmentJSON>(ContentJson.CaseInsensitive);
                            if (equipmentData != null) registeredItem = RegisterEquipmentItem(equipmentData, idString, entry);
                            break;
                        case "item":
                        default:
                            var itemData = itemElement.Deserialize<ItemJSON>(ContentJson.CaseInsensitive);
                            if (itemData != null) registeredItem = RegisterItem(itemData, idString);
                            break;
                    }

                    if (registeredItem != null)
                    {
                        if (ContentJson.TryGetProperty(itemElement, "Description", out var v))
                        {
                            registeredItem.Description = v.GetString();
                        }
                        registeredItem.Color = color;
                        registeredItem.Namespace = ModInfo.ModId;
                    }
                }
                catch (Exception exItem)
                {
                    Debug.Error($"[GameSetLoader] Error loading specific item: {exItem.Message}");
                }
            }

            RegisterEraserItem();
            RegisterEraserItem2();
        }
        catch (Exception ex)
        {
            Debug.Error($"[GamesetLoader] Critical error loading items: {ex.Message}");
        }
    }
    private static void SetBlocksDrop()
    {
        foreach (var block in GameAssets.Blocks.Values)
        {
            if (block == null) continue;
            if (block.Id <= 1) continue;

            var drop = block.Drop;
            var dropIdStr = drop.Item.Id_string;

            if (dropIdStr == "$self")
            {
                block.Drop.Item = block.AsItem;

            }
            else
            {

                var fullID = ResolveId(dropIdStr, SplitId(block.Id_string).modId);

                if (fullID == block.Id_string)
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

    private static void LoadRecipes(GameSetContent content)
    {
        try
        {
            foreach (var entry in content.Recipes)
            {
                var r = entry.Data.Deserialize<RecipeJSON>(ContentJson.CaseInsensitive);
                if (r == null) continue;

                var ingredientFullId = ResolveId(r.Ingredient.Item, entry.Namespace);
                var productFullId = ResolveId(r.Product.Item, entry.Namespace);

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

                GameAssetsRegister.RegisterRecipe(r, item, item2);
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[GameSetLoader] Error loading recipes: {ex.Message}");
        }
    }

    private static void LoadCrafting(GameSetContent content)
    {
        try
        {
            CraftingJSON crafting = content.CraftingJson().Deserialize<CraftingJSON>(ContentJson.CaseInsensitive);

            if (crafting == null)
            {
                Debug.Error("[GameSetLoader] Crafting was null!");
                return;
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
                                category.IconSprite = item.Icon;
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

    private static void LoadLoot(GameSetFiles gameSetFiles)
    {
        if (GameAssets.LootConfig == null)
        {
            GameAssets.LootConfig = new LootConfig();
        }

        try
        {
            foreach (var file in gameSetFiles.Folder("Generation/Loot"))
            {
                try
                {

                    string tableName = file.Name;

                    var items = GameSetFiles.Deserialize<List<LootEntry>>(file.Data);

                    if (items != null)
                    {
                        GameAssets.LootConfig.LootTables[tableName] = items;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Error($"[GameSetLoader] Error loading loot file '{file.Source}': {ex.Message}");
                }
            }

            GameAssets.LootConfig.Validate(ModInfo.ModId);
        }
        catch (Exception ex)
        {
            Debug.Error($"[GameSetLoader] Critical error in LoadLoot: {ex.Message}");
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

        foreach (var category in GameAssets.CraftingCategories.Values)
            GUI.CraftingCategoryOrder.PutBlocksLast(category.Items);
    }

    private static void LoadBlueprints(CraftingJSON data)
    {
        short id = 0;
        if (data == null) return;

        foreach (var e in data.Blueprints)
        {
            List<Ingredient> ingredient = new List<Ingredient>();
            Product product;
            Blueprint blueprint = new Blueprint();

            Item? item = null;

            if (e?.Product == null || string.IsNullOrWhiteSpace(e.Product.Item) || e.Ingredients == null)
            {
                Debug.Error($"[GameSetLoader] [Blueprints]: A blueprint without Product.Item or Ingredients was skipped");
                continue;
            }

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
                if (ing == null || string.IsNullOrWhiteSpace(ing.Item))
                {
                    Debug.Error($"[GameSetLoader] [Blueprints]: An ingredient without Item in the blueprint of {e.Product.Item} was skipped");
                    continue;
                }

                var ingName = ing.Item.ToLower();

                if (CraftingLogic.IsVirtualResource(ingName))
                {
                    item2 = new Item(255, ingName == "$health" ? "Health" : "Power", 0.5f);
                    item2.Id_string = ingName;
                    item2.Icon = AtlasSprite.None;

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

    private static Item RegisterWeaponItem(WeaponItemJSON data, string id)
    {
        data.MaxStack = 1;

        short projectileID = 0;
        data.Projectile = ResolveName(data.Projectile ?? "", NamespaceOf(id), name => GameAssets.TryGetProjectileByName(name, out _));
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
        weaponItem.ReloadSeconds = MathF.Max(0.05f, data.ReloadTime);
        weaponItem.AnimationSpeed = data.AnimationSpeed;
        weaponItem.AnimationPushback = data.AnimationPushback;
        weaponItem.Recoil = data.Recoil;
        weaponItem.Spread = data.Spread;
        weaponItem.Id_string = id;
        weaponItem.NeedsAmmo = !string.IsNullOrEmpty(data.Ammo);

        if (weaponItem.NeedsAmmo)
        {
            EnqueuePostLoadAction(() =>
            {
                weaponItem.Ammo = GameAssets.GetItemByFullID(ResolveId(data.Ammo, SplitId(weaponItem.Id_string).modId));
            });
        }

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



    private static Item RegisterDrillItem(DrillItemJSON data, string id)
    {
        data.MaxStack = 1;
        var drillItem = new DrillItem(
            (byte)data.MaxStack,
            data.Name,

            data.ModelDepth)
        {
            Tier = data.Tier,
            PowerUsage = (byte)data.PowerUsage,
            Range = data.Range,
            Damage = MathF.Max(0f, data.Damage),
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
        eraser.Namespace = "default";
        eraser.Description = "LMB - select block 1\nRMB - select block 2\nMMB - reset\nEnter - confirm";
        GameAssetsRegister.RegisterItem(eraser, "eraser");
    }
    private static void RegisterEraserItem2()
    {

        var camPoint = new CameraPointItem(
           "Camera Point",
           2f)
        {

        };
        camPoint.Id_string = "default:camera_point";
        camPoint.Namespace = "default";
        camPoint.Description = "LMB - remove last point\nMMB - remove all points\nRMB - add point\nEnter - start\nAlt+Scroll - set speed";
        GameAssetsRegister.RegisterItem(camPoint, "cameraPoint");
    }


    private static Item RegisterConsumableItem(ConsumableItemJSON data, string id)
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
            OxygenAmount = data.OxygenAmount,
            UseCooldown = data.UseCooldown,
            UseSound = data.Sound,
            Category = data.Category,
        };
        GameAssetsRegister.RegisterItem(consumableItem, data.Sprite);

        return consumableItem;
    }

    private static Item RegisterRepairToolItem(RepairToolItemJSON data, string id)
    {
        data.ValidateMaxStack();
        var tool = new RepairToolItem((byte)Math.Max(1, data.MaxStack), data.Name, data.ModelDepth)
        {
            Id_string = id,
            Category = data.Category,
            RepairTime = Math.Clamp(data.RepairTime, 0.05f, 60f),
            PowerPerSecond = Math.Clamp(data.PowerPerSecond, 0f, 100f),
            Range = Math.Clamp(data.Range, 1f, 12f)
        };
        GameAssetsRegister.RegisterItem(tool, data.Sprite);
        return tool;
    }

    private static Item RegisterMobSpawnerItem(MobSpawnerItemJSON data, string id)
    {
        data.ValidateMaxStack();
        var spawnerItem = new MobSpawnerItem((byte)data.MaxStack, data.Name, data.ModelDepth)
        {
            Id_string = id,
            Category = data.Category,
            MobId = data.Mob,
            UseCooldown = data.UseCooldown,
        };
        GameAssetsRegister.RegisterItem(spawnerItem, data.Sprite);

        return spawnerItem;
    }

    private static Item RegisterPictureItem(PictureItemJSON data, string id, ContentEntry entry)
    {
        string picturePath = ResolveGameSetFile(data.Picture, entry);
        if (picturePath.Length == 0)
            Debug.Error($"[GameSetLoader] Picture file <{data.Picture}> for item <{data.Name}> was not found");

        var picture = new PictureItem(data.Name, data.ModelDepth)
        {
            Id_string = id,
            Category = data.Category,
            PicturePath = picturePath,
            Width = PictureFrameBlockData.ClampSize(data.Width),
            Height = PictureFrameBlockData.ClampSize(data.Height)
        };
        GameAssetsRegister.RegisterItem(picture, data.Sprite);
        return picture;
    }

    private static Item RegisterEquipmentItem(EquipmentJSON data, string id, ContentEntry entry)
    {
        if (!Enum.TryParse(data.Slot, true, out EquipmentSlotType slot))
        {
            Debug.Error($"[GameSetLoader] RegisterEquipmentItem: unknown slot <{data.Slot}> for item <{data.Name}>");
            return null;
        }

        if (data.ExplosionProtection < 0f || data.ExplosionProtection > 100f)
            Debug.Warning($"[GameSetLoader] RegisterEquipmentItem: ExplosionProtection of <{data.Name}> must be 0-100, clamped");

        var equipmentItem = new EquipmentItem(data.Name, data.ModelDepth)
        {
            Id_string = id,
            Category = data.Category,
            Slot = slot,
            HealthBonus = data.Health,
            PowerBonus = data.Power,
            ExplosionProtection = Spacebox.Game.Player.ArmorProtection.FromPercent(data.ExplosionProtection),
            Flashlight = data.Flashlight,
            Radar = data.Radar,
            ArmorModelPath = ResolveGameSetFile(data.ArmorModel, entry),
            ArmorTexturePath = ResolveGameSetFile(data.ArmorTexture, entry),
        };
        GameAssetsRegister.RegisterItem(equipmentItem, data.Sprite);

        return equipmentItem;
    }

    private static string ResolveGameSetFile(string relativePath, ContentEntry entry)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || ModInfo == null) return "";

        string own = entry?.ResolveFile(relativePath) ?? "";
        if (own.Length > 0) return own;

        string path = Path.Combine(ModInfo.ModPath, relativePath);
        return File.Exists(path) ? path : "";
    }

    private static Item RegisterItem(ItemJSON data, string id)
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


            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Error loading settings: {ex.Message}");
            }
        }
    }

    private const string LightingFile = "Optional/lighting.json";

    private static void ApplyExtensionSettings(GameSetStack stack)
    {
        foreach (var extension in stack.Extensions)
        {
            var config = ContentJson.ParseFile(Path.Combine(extension.Folder, GameSetFormat.ConfigFile), null) as System.Text.Json.Nodes.JsonObject;
            if (config != null && ContentJson.Get(config, nameof(ModConfig.OxygenConsumeInterval)) != null)
                ModInfo.OxygenConsumeInterval = extension.Config.OxygenConsumeInterval;
        }
    }

    private static void LoadLighting(LayeredFile file)
    {
        if (file != null)
        {
            try
            {

                Modlighting settings = GameSetFiles.Deserialize<Modlighting>(file.Data);

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
            Debug.Log($"[GameSetLoader] {fileName} not found in the GameSet, using the one of the GameSet it is built on.");
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
        public float OxygenConsumeInterval { get; set; } = 10f;
        [System.Text.Json.Serialization.JsonConverter(typeof(Spacebox.Game.Resource.Content.StringOrListConverter))]
        public string Extends { get; set; } = "";
        public string MinBaseVersion { get; set; } = "";
        public string MaxBaseVersion { get; set; } = "";
        public List<string> Includes { get; set; } = new List<string>();
        [System.Text.Json.Serialization.JsonIgnore]
        public string FolderName { get; set; } = "";
        [System.Text.Json.Serialization.JsonIgnore]
        public string ModPath { get; set; } = "";
        [System.Text.Json.Serialization.JsonIgnore]
        public Texture2D? Icon { get; set; } = null;

        public List<DefaultItem> ItemsOnStart { get; set; } = new List<DefaultItem>();

        public void Dispose()
        {
            Icon?.Dispose();
            ItemsOnStart.Clear();
        }
    }

    public class Modlighting
    {
        public Color3Byte AmbientColor { get; set; } = Color3Byte.White;
        public Color3Byte FogColor { get; set; } = Color3Byte.Black;
        public int FogDensity { get; set; } = 10;
    }

    public class DefaultItem
    {
        public string ID { get; set; } = "unknown";
        public byte Count { get; set; } = 0;
        public string PutIn { get; set; } = StartItemPlacement.Auto;
        public int SlotId { get; set; } = -1;

    }

    public static void GiveStartItems(LocalAstronaut player, Dictionary<string, Item> allGameItems)
    {
        if (player?.GameMode == GameMode.Spectator ||
            ModInfo?.ItemsOnStart?.Count == 0 ||
            allGameItems?.Count == 0)
            return;

        foreach (var itemData in ModInfo.ItemsOnStart)
        {
            if (itemData.Count < 1 || string.IsNullOrEmpty(itemData.ID))
                continue;

            if (ModInfo.ModId == "") Debug.Error("[GameSetLoader][GiveStartItems] ModId is empty! Cannot give start items!");

            string fullId = CombineId(ModInfo.ModId, itemData.ID);

            if (GameAssets.TryGetItemByFullID(fullId, out Item item))
            {
                if (!StartItemPlacement.Give(player, item, itemData.Count, itemData.PutIn, itemData.SlotId))
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

    public static void GiveStartEquipment(LocalAstronaut player)
    {
        if (ModInfo?.ItemsOnStart == null) return;

        foreach (var itemData in ModInfo.ItemsOnStart)
        {
            if (string.IsNullOrEmpty(itemData.ID)) continue;
            if (GameAssets.TryGetItemByFullID(CombineId(ModInfo.ModId, itemData.ID), out Item item))
                player.Equipment.TryEquipIfEmpty(item);
        }
    }

}
