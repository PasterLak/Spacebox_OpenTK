using Engine;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using Spacebox.GUI;

namespace Spacebox.Game
{
    public static class GameAssetsRegister
    {
        public static void RegisterBlock(BlockData blockData)
        {
            GameAssets.IncrementBlockId(blockData);
            GameAssets.Blocks.Add(blockData.Id, blockData);
            GameAssets.AddBlockString(blockData.Id_string, blockData);

            if (blockData.Id > 1)
                BlockData.CacheUvs(blockData);

            RegisterItem(blockData);
            CreateDust(blockData);
        }

        public static void RegisterRecipe(RecipeJSON recipeData, Item item, Item item2)
        {
            recipeData.Type = recipeData.Type.ToLower();
            if (!GameAssets.Recipes.ContainsKey(recipeData.Type))
                GameAssets.Recipes.Add(recipeData.Type, new Dictionary<short, Recipe>());
            if (!GameAssets.Recipes[recipeData.Type].ContainsKey(item.Id))
            {
                Recipe recipe = new Recipe();
                recipe.RequiredTicks = (short)(Time.SecondsToTicks(recipeData.DurationSec));
                recipe.PowerPerTickRequared = (short)recipeData.PowerPerTickRequared;
                recipe.Ingredient = new Ingredient(item, (byte)recipeData.Ingredient.Quantity);
                recipe.Product = new Product(item2, (byte)recipeData.Product.Quantity);
                GameAssets.Recipes[recipeData.Type].Add(item.Id, recipe);
            }
        }

        public static void RegisterItem(BlockData blockData)
        {
            try
            {
                BlockItem item = new BlockItem(blockData.Id, 0, 64, blockData.Name, blockData.Mass, blockData.Durability);
                item.Mass = blockData.Mass;
                item.Category = blockData.Category;
                blockData.AsItem = item;
                item.Description = blockData.Description;
                item.Id_string = blockData.Id_string;
                GameAssets.IncrementItemId(blockData.AsItem);
                GameAssets.Items.Add(item.Id, item);
                GameAssets.AddItemString(blockData.Id_string, item);
                if (!Application.IsHeadless) CacheIcon(blockData);
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameAssetsRegister] Error registering block item {blockData.Name}: {ex.Message}");
            }
        }

        public static void RegisterItem(Item item, string spriteId)
        {
            try
            {
                GameAssets.IncrementItemId(item);
                GameAssets.Items.Add(item.Id, item);
                GameAssets.AddItemString(item.Id_string, item);
                string spriteName = spriteId.ToLowerInvariant();
                if (!GameAssets.AtlasItems.Contains(spriteName))
                    Debug.Error($"[GameAssetsRegister] Sprite <{spriteName}> for item <{item.Name}> was not found");

                item.Icon = GameAssets.AtlasItems.GetSprite(spriteName);
                GenerateItemModel(spriteName, item);
                GenerateItemWorldModel(spriteName, item);
                if (item is ConsumableItem consumable)
                {
                    if (!GameAssets.ItemSounds.ContainsKey(consumable.Id) && GameAssets.Sounds.ContainsKey(consumable.UseSound))
                        GameAssets.ItemSounds.Add(consumable.Id, GameAssets.Sounds[consumable.UseSound]);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameAssetsRegister] Error registering item {item.Name}: {ex.Message}");
            }

        }

        private static void GenerateItemWorldModel(string spriteName, Item item)
        {
            Model model = ItemModelGenerator.GenerateWorldModel(
                CreateItemTexture(spriteName),
               ItemMuzzle.WorldDepth(item)
                );
            GameAssets.ItemWorldModels.Add(item.Id, model);
        }


        private static void GenerateItemModel(string spriteName, Item item)
        {
            bool isAnimated = item is DrillItem || item is WeaponItem;
            var texture = CreateItemTexture(spriteName);
            var emission = GameAssets.AtlasItems.CreateLayerTexture(GameAtlasFactory.EmissionLayer, spriteName)
                           ?? Texture2D.CreateTexture(texture.Width, texture.Height, new Color4(0, 0, 0, 0), FilterMode.Nearest);
            ItemModel model = ItemModelGenerator.GenerateItemModel(
                texture,
                emission,
                ItemMuzzle.FirstPersonPixelSize,
                ItemMuzzle.FirstPersonDepth(item),
                isAnimated,
                true);
            GameAssets.ItemModels.Add(item.Id, model);
            if (item is WeaponItem && ItemMuzzle.TryFromPixels(texture.GetPixels(), out var muzzle)) GameAssets.ItemMuzzles[item.Id] = muzzle;
        }

        private static Texture2D CreateItemTexture(string spriteName)
        {
            return GameAssets.AtlasItems.CreateTexture(spriteName) ?? new Texture2D(32, 32, true);
        }

        private static void CacheIcon(BlockData blockData)
        {
            if (BlockIconRenderer.NeedsModelIcon(blockData))
            {
                AddIcon(blockData, BlockIconRenderer.Render(blockData));
                return;
            }

            var blocks = GameAssets.AtlasBlocks;
            if (!blocks.TryGetPixels(blockData.GetFaceTextureName(Generation.Blocks.Direction.Left), true, out var left) ||
                !blocks.TryGetPixels(blockData.GetFaceTextureName(Generation.Blocks.Direction.Forward), true, out var forward) ||
                !blocks.TryGetPixels(blockData.GetFaceTextureName(Generation.Blocks.Direction.Up), true, out var top))
                return;

            int faceSize = Math.Min(MinSide(left), Math.Min(MinSide(forward), MinSide(top)));
            var pixels = IsometricIcon.CreatePixels(Resample(left, faceSize), Resample(forward, faceSize), Resample(top, faceSize));
            if (pixels == null) return;

            AddIcon(blockData, pixels);
        }

        private static void AddIcon(BlockData blockData, Color4[,] pixels)
        {
            string iconName = GameAtlasFactory.BlockIconName(blockData);
            GameAssets.AtlasItems.Add(iconName, pixels, true);
            blockData.AsItem.Icon = GameAssets.AtlasItems.GetSprite(iconName);
        }

        private static int MinSide(Color4[,] pixels)
        {
            return Math.Min(pixels.GetLength(0), pixels.GetLength(1));
        }

        private static Color4[,] Resample(Color4[,] source, int size)
        {
            int width = source.GetLength(0);
            int height = source.GetLength(1);
            if (width == size && height == size) return source;

            var result = new Color4[size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    result[x, y] = source[x * width / size, y * height / size];

            return result;
        }

        private static void CreateDust(BlockData block)
        {
            var face = GameAssets.AtlasBlocks.CreateTexture(block.GetFaceTextureName(Generation.Blocks.Direction.Forward, BlockState.Inactive));
            Texture2D texture = face != null
                ? BlockDestructionTexture.Generate(face)
                : Texture2D.CreateTexture(8, 8, new Color4(0, 0, 0, 0), FilterMode.Nearest);
            face?.Dispose();
            texture.Name = block.Id_string + "_dust";
            GameAssets.BlockDusts.Add(block.Id, texture);
        }
    }
}
