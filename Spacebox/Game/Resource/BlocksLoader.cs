using Engine;
using Engine.Utils;
using OpenTK.Mathematics;


namespace Spacebox.Game.Resource
{
    public static class BlocksLoader
    {
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
            AddAirBlock();
            string blocksFile = GameSetLoader.GetFilePath(modPath, defaultModPath, "blocks.json");
            if (blocksFile == null) return;

            try
            {
               
                List<BlockDataJSON> blocks = JsonFixer.LoadJsonSafe<List<BlockDataJSON>>(blocksFile);

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

        private static bool ProcessBlock(BlockDataJSON block)
        {
            if (string.IsNullOrWhiteSpace(block.ID))
            {
                Debug.Error("[GameSetLoader] Block has empty ID and was skipped");
                return false;
            }

            if (string.IsNullOrWhiteSpace(block.Name))
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has empty name and was skipped");
                return false;
            }

            NormalizeBlockStrings(block);

            if (!ValidateBlockType(block))
                return false;

            if (!ValidateBlockValues(block))
                return false;

            var blockId = GameSetLoader.ValidateIdString(GameSetLoader.ModInfo.ModId, block.ID);
            blockId = GameSetLoader.CombineId(GameSetLoader.ModInfo.ModId, blockId);

            if (GameAssets.HasItem(blockId))
            {
                Debug.Error($"[GameSetLoader] Block '{blockId}' already exists and was skipped");
                return false;
            }

            try
            {
                var blockData = CreateBlockData(block, blockId);
                GameAssetsRegister.RegisterBlock(blockData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Failed to create block '{block.ID}': {ex.Message}");
                return false;
            }
        }

        private static void NormalizeBlockStrings(BlockDataJSON block)
        {
            block.Type = block.Type?.ToLower() ?? "block";
            block.Sides = block.Sides?.ToLower() ?? "";
            block.Up = block.Up?.ToLower() ?? "";
            block.Down = block.Down?.ToLower() ?? "";
            block.Left = block.Left?.ToLower() ?? "";
            block.Right = block.Right?.ToLower() ?? "";
            block.Forward = block.Forward?.ToLower() ?? "";
            block.Back = block.Back?.ToLower() ?? "";
            block.Category = block.Category?.ToLower() ?? "";
            block.Description = block.Description ?? "";
            block.SoundPlace = block.SoundPlace ?? "blockPlaceDefault";
            block.SoundDestroy = block.SoundDestroy ?? "blockDestroyDefault";
        }

        private static bool ValidateBlockType(BlockDataJSON block)
        {
            if (!BlockFactory.ValidateBlockType(block.Type))
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid type '{block.Type}' and was skipped");
                Debug.Error($"[GameSetLoader] Valid types are: {string.Join(", ", BlockFactory.GetBlockTypes())}");
                return false;
            }
            return true;
        }

        private static bool ValidateBlockValues(BlockDataJSON block)
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

        private static BlockData CreateBlockData(BlockDataJSON block, string blockId)
        {
            bool sameSides = string.IsNullOrEmpty(block.Up) && string.IsNullOrEmpty(block.Down) &&
                             string.IsNullOrEmpty(block.Left) && string.IsNullOrEmpty(block.Right) &&
                             string.IsNullOrEmpty(block.Forward) && string.IsNullOrEmpty(block.Back);

            bool hasLightColor = block.LightColor != Color3Byte.Black;
            var blockColor = hasLightColor ? block.LightColor.ToVector3() : Vector3.Zero;

            var blockData = new BlockData(block.Name, block.Type, new Vector2Byte(0, 0), block.IsTransparent, blockColor)
            {
                AllSidesAreSame = sameSides,
                Id_string = blockId,
                Description = block.Description,
                Mass = (byte)Math.Clamp(block.Mass, 1, byte.MaxValue),
                Durability = (byte)Math.Clamp(block.Durability, 1, byte.MaxValue),
                PowerToDrill = (byte)Math.Clamp(block.PowerToDrill, 0, byte.MaxValue),
                Efficiency = Math.Max(0.1f, block.Efficiency),
                Category = block.Category,
                DropIDFull = string.IsNullOrWhiteSpace(block.Drop) ? "$self" : block.Drop,
                DropQuantity = (byte)Math.Clamp(block.DropQuantity, 1, byte.MaxValue),
                Sides = block.Sides,
                Up = string.IsNullOrEmpty(block.Up) ? block.Sides : block.Up,
                Down = string.IsNullOrEmpty(block.Down) ? block.Sides : block.Down,
                Left = string.IsNullOrEmpty(block.Left) ? block.Sides : block.Left,
                Right = string.IsNullOrEmpty(block.Right) ? block.Sides : block.Right,
                Forward = string.IsNullOrEmpty(block.Forward) ? block.Sides : block.Forward,
                Back = string.IsNullOrEmpty(block.Back) ? block.Sides : block.Back
            };

            GiveBlockSounds(blockData, block);
            return blockData;
        }

        private static void GiveBlockSounds(BlockData blockData, BlockDataJSON modBlockData)
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
