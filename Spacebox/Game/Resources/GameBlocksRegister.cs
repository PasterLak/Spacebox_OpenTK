using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Resources;
using Engine;
using Spacebox.GUI;

namespace Spacebox.Game
{
    public static class GameAssetsRegister
    {
        public static void RegisterBlock(BlockData blockData)
        {
            GameAssets.IncrementBlockId(blockData);
            GameAssets.Blocks.Add(blockData.Id, blockData);
            CacheBlockUVs(blockData);
            blockData.CacheUVsByDirection();
            RegisterItem(blockData);
            CreateDust(blockData);
        }

        public static void RegisterRecipe(RecipeData recipeData, Item item, Item item2)
        {
            recipeData.Type = recipeData.Type.ToLower();
            if (!GameAssets.Recipes.ContainsKey(recipeData.Type))
                GameAssets.Recipes.Add(recipeData.Type, new Dictionary<short, Recipe>());
            if (!GameAssets.Recipes[recipeData.Type].ContainsKey(item.Id))
            {
                Recipe recipe = new Recipe();
                recipe.RequiredTicks = (short)recipeData.RequiredTicks;
                recipe.PowerPerTickRequared = (short)recipeData.PowerPerTickRequared;
                recipe.Ingredient = new Ingredient(item, (byte)recipeData.Ingredient.Quantity);
                recipe.Product = new Product(item2, (byte)recipeData.Product.Quantity);
                GameAssets.Recipes[recipeData.Type].Add(item.Id, recipe);
            }
        }

        public static void RegisterItem(BlockData blockData)
        {
           
            BlockItem item = new BlockItem(blockData.Id, 0, 64, blockData.Name, blockData.Mass, blockData.Durability);
            item.Mass = blockData.Mass;
            item.Category = blockData.Category;
            blockData.Item = item;
            GameAssets.IncrementItemId(blockData.Item);
            GameAssets.Items.Add(item.Id, item);
            CacheIcon(blockData);
        }

        public static void RegisterItem(Item item, string spriteName)
        {
            GameAssets.IncrementItemId(item);
            GameAssets.Items.Add(item.Id, item);
            var uvIndex = GameAssets.AtlasItems.GetUVIndexByName(spriteName.ToLower());
            byte coordX = uvIndex.X;
            byte coordY = uvIndex.Y;
            item.TextureCoord = new Vector2i(coordX, coordY);
            CacheIcon(item, coordX, coordY);
            GenerateItemModel(coordX, coordY, item);
            if (item is ConsumableItem consumable)
            {
                if (!GameAssets.ItemSounds.ContainsKey(consumable.Id) && GameAssets.Sounds.ContainsKey(consumable.UseSound))
                    GameAssets.ItemSounds.Add(consumable.Id, GameAssets.Sounds[consumable.UseSound]);
            }
        }

        private static void GenerateItemModel(byte coordX, byte coordY, Item item)
        {
            bool isAnimated = item is DrillItem || item is WeaponItem;
            ItemModel model = ItemModelGenerator.GenerateModel(
                GameAssets.ItemsTexture,
                coordX,
                coordY,
                0.004f,
                item.ModelDepth / 500f * 2f,
                isAnimated,
                true);
            GameAssets.ItemModels.Add(item.Id, model);
        }

        private static void CacheIcon(BlockData blockData)
        {
            Texture2D texture = IsometricIcon.CreateIsometricIcon(
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.WallsUVIndex, GameAssets.AtlasBlocks.SizeBlocks),
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.TopUVIndex, GameAssets.AtlasBlocks.SizeBlocks));
            texture.UpdateTexture(true);
            blockData.Item.IconTextureId = texture.Handle;
            GameAssets.ItemIcons.Add(blockData.Item.Id, texture);
        }

        private static void CacheIcon(Item item, byte x, byte y)
        {
            Texture2D texture = UVAtlas.GetBlockTexture(GameAssets.ItemsTexture, x, y, GameAssets.AtlasItems.SizeBlocks);
            texture.FlipY();
            texture.UpdateTexture(true);
            item.IconTextureId = texture.Handle;
            GameAssets.ItemIcons.Add(item.Id, texture);
        }

        private static void CacheBlockUVs(BlockData block)
        {
            ProcessBlockDataTexture(block);
        }

        private static void ProcessBlockDataTexture(BlockData blockData)
        {
            if (GameAssets.AtlasBlocks == null)
            {
                Debug.Error("[GameBlocksRegistrar] AtlasTexture is not created!");
                return;
            }
            blockData.WallsUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Sides);
            blockData.TopUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Top);
            blockData.BottomUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Bottom);
            blockData.WallsUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Sides);
            blockData.TopUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Top);
            blockData.BottomUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Bottom);
        }

        private static void CreateDust(BlockData block)
        {
            Texture2D texture = BlockDestructionTexture.Generate(block.WallsUVIndex);
            GameAssets.BlockDusts.Add(block.Id, texture);
        }
    }
}
