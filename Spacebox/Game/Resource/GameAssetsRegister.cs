using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Resource;
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
            GameAssets.AddBlockString(blockData.Id_string, blockData);
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
                CacheIcon(blockData);
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameAssetsRegister] Error registering block item {blockData.Name}: {ex.Message}");
            }
        }

        public static void RegisterItem(Item item, string spriteName)
        {
            try
            {
                GameAssets.IncrementItemId(item);
                GameAssets.Items.Add(item.Id, item);
                GameAssets.AddItemString(item.Id_string, item);
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
            catch (Exception ex)
            {
                Debug.Error($"[GameAssetsRegister] Error registering item {item.Name}: {ex.Message}");
            }

        }

        private static void GenerateItemModel(byte coordX, byte coordY, Item item)
        {
            bool isAnimated = item is DrillItem || item is WeaponItem;
            ItemModel model = ItemModelGenerator.GenerateModelFromAtlas(
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
            Texture2D texture = IsometricIcon.Create(
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.LeftUVIndex, GameAssets.AtlasBlocks.SizeBlocks),
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.ForwardUVIndex, GameAssets.AtlasBlocks.SizeBlocks),
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.UpUVIndex, GameAssets.AtlasBlocks.SizeBlocks));

            texture.FilterMode = FilterMode.Nearest;
            blockData.AsItem.IconTextureId = texture.Handle;
            GameAssets.ItemIcons.Add(blockData.AsItem.Id, texture);
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
            if (blockData.Id == 0)
            {
                return;
            }

            blockData.WallsUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Sides);
            blockData.WallsUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Sides);

            blockData.UpUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Up);
            blockData.UpUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Up);

            blockData.DownUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Down);
            blockData.DownUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Down);

            blockData.LeftUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Left);
            blockData.LeftUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Left);

            blockData.RightUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Right);
            blockData.RightUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Right);

            blockData.ForwardUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Forward);
            blockData.ForwardUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Forward);

            blockData.BackUV = GameAssets.AtlasBlocks.GetUVByName(blockData.Back);
            blockData.BackUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(blockData.Back);

            if (blockData.Left == blockData.Sides &&
                blockData.Right == blockData.Sides &&
                blockData.Forward == blockData.Sides &&
                blockData.Back == blockData.Sides)
            {
                blockData.LeftUV = blockData.WallsUV;
                blockData.RightUV = blockData.WallsUV;
                blockData.ForwardUV = blockData.WallsUV;
                blockData.BackUV = blockData.WallsUV;

                blockData.LeftUVIndex = blockData.WallsUVIndex;
                blockData.RightUVIndex = blockData.WallsUVIndex;
                blockData.ForwardUVIndex = blockData.WallsUVIndex;
                blockData.BackUVIndex = blockData.WallsUVIndex;
            }
        }

        private static void CreateDust(BlockData block)
        {
            Texture2D texture = BlockDestructionTexture.Generate(block.WallsUVIndex);
            GameAssets.BlockDusts.Add(block.Id, texture);
        }
    }
}
