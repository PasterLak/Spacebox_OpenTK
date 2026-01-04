using Engine;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using System.Drawing;


namespace Spacebox.Game.Resource
{
    public static class BlocksLoader
    {
        private static void AddVoidBlock()
        {
            var _void = new BlockData("Void", "block", new Vector2Byte(0, 0)); // for future TRANSPARENT true
            _void.Mass = 0;
            _void.Category = "";
            _void.Sides = "sand";
            _void.Id_string = "default:void";
       

            GameAssetsRegister.RegisterBlock(_void);
        }

        private static void AddAirBlock()
        {
            var air = new BlockData("Air", "block", new Vector2Byte(0, 0));
            air.Mass = 0;
            air.Category = "";
            air.Sides = "sand";
            air.Id_string = "default:air";

            GameAssetsRegister.RegisterBlock(air);
        }

        public static void LoadBlocks(string modPath, string defaultModPath)
        {
            AddVoidBlock();
            AddAirBlock();
            string blocksFile = GameSetLoader.GetFilePath(modPath, defaultModPath, "blocks.json");
            if (blocksFile == null) return;

            try
            {

                List<BlockJSON> blocks = JsonFixer.LoadJsonSafe<List<BlockJSON>>(blocksFile);

                if (blocks == null)
                {
                    Debug.Error("[GameSetLoader] Failed to parse blocks.json file");
                    return;
                }

                foreach (var block in blocks)
                {
                    if (!ProcessBlock(block))
                        continue;
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Error loading blocks: {ex.Message}");
            }
        }

        private static bool ProcessBlock(BlockJSON blockJson)
        {
            if (string.IsNullOrWhiteSpace(blockJson.ID))
            {
                Debug.Error("[GameSetLoader] Block has empty ID and was skipped");
                return false;
            }

            if (string.IsNullOrWhiteSpace(blockJson.Name))
            {
                Debug.Error($"[GameSetLoader] Block '{blockJson.ID}' has empty name and was skipped");
                return false;
            }

            NormalizeBlockStrings(blockJson);

            if (!ValidateBlockType(blockJson))
                return false;

            if (!ValidateBlockValues(blockJson))
                return false;

            var blockId = GameSetLoader.ValidateIdString(GameSetLoader.ModInfo.ModId, blockJson.ID);
            blockId = GameSetLoader.CombineId(GameSetLoader.ModInfo.ModId, blockId);

            if (GameAssets.HasItem(blockId))
            {
                Debug.Error($"[GameSetLoader] Block '{blockId}' already exists and was skipped");
                return false;
            }

            try
            {
                BlockData blockData = CreateBlockData(blockJson, blockId);

                if (blockData.Type == "storage")
                {
                    var Size = blockJson.StorageSize;
                    if (Size.X < 1) Size = new Vector2Byte((byte)1, Size.Y);
                    if (Size.Y < 1) Size = new Vector2Byte(Size.X, (byte)1);

                    blockData = new StorageBlockData(blockData, Size);


                }

                GameAssetsRegister.RegisterBlock(blockData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Failed to create block '{blockJson.ID}': {ex.Message}");
                return false;
            }
        }

        private static void NormalizeBlockStrings(BlockJSON block)
        {
            block.Type = block.Type?.ToLower() ?? "block";
            block.Sides = block.Sides?.ToLower() ?? "";
            block.Up = block.Up?.ToLower() ?? "";
            block.Down = block.Down?.ToLower() ?? "";
            block.Left = block.Left?.ToLower() ?? "";
            block.Right = block.Right?.ToLower() ?? "";
            block.Forward = block.Forward?.ToLower() ?? "";
            block.Back = block.Back?.ToLower() ?? "";

            block.SidesOff = block.Sides?.ToLower() ?? "";
            block.UpOff = block.Up?.ToLower() ?? "";
            block.DownOff = block.Down?.ToLower() ?? "";
            block.LeftOff = block.Left?.ToLower() ?? "";
            block.RightOff = block.Right?.ToLower() ?? "";
            block.ForwardOff = block.Forward?.ToLower() ?? "";
            block.BackOff = block.Back?.ToLower() ?? "";

            block.Category = block.Category?.ToLower() ?? "";
            block.Description = block.Description ?? "";
            block.SoundPlace = block.SoundPlace ?? "blockPlaceDefault";
            block.SoundDestroy = block.SoundDestroy ?? "blockDestroyDefault";
        }

        private static bool ValidateBlockType(BlockJSON block)
        {
            if (!BlockFactory.ValidateBlockType(block.Type))
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid type '{block.Type}' and was skipped");
                Debug.Error($"[GameSetLoader] Valid types are: {string.Join(", ", BlockFactory.GetBlockTypes())}");
                return false;
            }
            return true;
        }

        private static bool ValidateBlockValues(BlockJSON block)
        {
            bool isValid = true;

            if (block.Mass <= 0)
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid mass {block.Mass}, setting to 1");
                block.Mass = 1;
                isValid = false;
            }

            if (block.Durability <= 0)
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid durability {block.Durability}, setting to 1");
                block.Durability = 1;
                isValid = false;
            }

            if (block.PowerToDrill < 0)
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid PowerToDrill {block.PowerToDrill}, setting to 0");
                block.PowerToDrill = 0;
                isValid = false;
            }

            if (block.Efficiency <= 0)
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid efficiency {block.Efficiency}, setting to 1.0");
                block.Efficiency = 1.0f;
                isValid = false;
            }

            if (block.DropQuantity <= 0)
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid DropQuantity {block.DropQuantity}, setting to 1");
                block.DropQuantity = 1;
                isValid = false;
            }

            return isValid;
        }

        private static BlockData CreateBlockData(BlockJSON block, string blockId)
        {


            bool hasLightColor = block.LightColor != Color3Byte.Black;
            var blockColor = hasLightColor ? block.LightColor.ToVector3() : Vector3.Zero;

            var blockData = new BlockData(block.Name, block.Type, new Vector2Byte(0, 0), block.IsTransparent, blockColor)
            {

                Id_string = blockId,
                Description = block.Description,
                Mass = (byte)Math.Clamp(block.Mass, 1, byte.MaxValue),
                Durability = (byte)Math.Clamp(block.Durability, 1, byte.MaxValue),
                PowerToDrill = (byte)Math.Clamp(block.PowerToDrill, 0, byte.MaxValue),
                Efficiency = Math.Max(0.1f, block.Efficiency),
                Category = block.Category,

                Sides = block.Sides
            };

            blockData.Drop.Item.Id_string = string.IsNullOrWhiteSpace(block.Drop) ? "$self" : block.Drop;
            blockData.Drop.Count = (byte)Math.Clamp(block.DropQuantity, 1, byte.MaxValue);

            blockData.SetFaceTexture(Direction.Up, string.IsNullOrEmpty(block.Up) ? block.Sides : block.Up);
            blockData.SetFaceTexture(Direction.Down, string.IsNullOrEmpty(block.Down) ? block.Sides : block.Down);
            blockData.SetFaceTexture(Direction.Left, string.IsNullOrEmpty(block.Left) ? block.Sides : block.Left);
            blockData.SetFaceTexture(Direction.Right, string.IsNullOrEmpty(block.Right) ? block.Sides : block.Right);
            blockData.SetFaceTexture(Direction.Forward, string.IsNullOrEmpty(block.Forward) ? block.Sides : block.Forward);
            blockData.SetFaceTexture(Direction.Back, string.IsNullOrEmpty(block.Back) ? block.Sides : block.Back);

            GiveBlockSounds(blockData, block);
            return blockData;
        }

        private static void GiveBlockSounds(BlockData blockData, BlockJSON modBlockData)
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
    }
}
