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

            if(blockData.Id != 0)
                BlockData.CacheUvs(blockData);
           
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
                 GameAssets.EmissionItems,
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
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.GetFaceUVIndex(Generation.Blocks.Direction.Left), GameAssets.AtlasBlocks.SizeBlocks),
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.GetFaceUVIndex(Generation.Blocks.Direction.Forward), GameAssets.AtlasBlocks.SizeBlocks),
                UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, blockData.GetFaceUVIndex(Generation.Blocks.Direction.Up), GameAssets.AtlasBlocks.SizeBlocks));

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

        private static void CreateDust(BlockData block)
        {
            Texture2D texture = BlockDestructionTexture.Generate(block.WallsUVIndex);
            GameAssets.BlockDusts.Add(block.Id, texture);
        }
    }
}
