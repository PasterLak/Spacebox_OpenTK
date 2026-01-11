using Engine;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Resource
{
    public static class BlocksLoader
    {
        private static void AddVoidBlock()
        {
            var _void = new BlockData("Void", "block", false);
            _void.Mass = 0;
            _void.Category = "";
            _void.SetTextureAllSides("sand");
            _void.Id_string = "default:void";

            GameAssetsRegister.RegisterBlock(_void);
        }

        private static void AddAirBlock()
        {
            var air = new BlockData("Air", "block", true);
            air.Mass = 0;
            air.Category = "";
            air.SetTextureAllSides("sand");
            air.Id_string = "default:air";

            GameAssetsRegister.RegisterBlock(air);
        }

        public static void LoadBlocks(string modPath, string defaultModPath)
        {

            if (!GameAssets.HasItem("default:void")) AddVoidBlock();
            if (!GameAssets.HasItem("default:air")) AddAirBlock();

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

            if (!ValidateBlockType(blockJson)) return false;
            if (!ValidateBlockValues(blockJson)) return false;

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
            block.Type = (block.Type ?? "block").ToLower();

            block.Sides = (block.Sides ?? "").ToLower();
            block.Up = (block.Up ?? "").ToLower();
            block.Down = (block.Down ?? "").ToLower();
            block.Left = (block.Left ?? "").ToLower();
            block.Right = (block.Right ?? "").ToLower();
            block.Forward = (block.Forward ?? "").ToLower();
            block.Back = (block.Back ?? "").ToLower();

            block.SidesOff = (block.SidesOff ?? "").ToLower();
            block.UpOff = (block.UpOff ?? "").ToLower();
            block.DownOff = (block.DownOff ?? "").ToLower();
            block.LeftOff = (block.LeftOff ?? "").ToLower();
            block.RightOff = (block.RightOff ?? "").ToLower();
            block.ForwardOff = (block.ForwardOff ?? "").ToLower();
            block.BackOff = (block.BackOff ?? "").ToLower();

            block.Category = (block.Category ?? "").ToLower();
            block.Description = block.Description ?? "";
            block.SoundPlace = block.SoundPlace ?? "blockPlaceDefault";
            block.SoundDestroy = block.SoundDestroy ?? "blockDestroyDefault";
        }

        private static bool ValidateBlockType(BlockJSON block)
        {
            if (!BlockFactory.ValidateBlockType(block.Type))
            {
                Debug.Error($"[GameSetLoader] Block '{block.ID}' has invalid type '{block.Type}' and was skipped. Valid: {string.Join(", ", BlockFactory.GetBlockTypes())}");
                return false;
            }
            return true;
        }

        private static bool ValidateBlockValues(BlockJSON block)
        {
            bool isValid = true;

            if (block.Mass <= 0) { Debug.Error($"[GameSetLoader] Block '{block.ID}' invalid mass {block.Mass}, set to 1"); block.Mass = 1; isValid = false; }
            if (block.Durability <= 0) { Debug.Error($"[GameSetLoader] Block '{block.ID}' invalid durability {block.Durability}, set to 1"); block.Durability = 1; isValid = false; }
            if (block.PowerToDrill < 0) { Debug.Error($"[GameSetLoader] Block '{block.ID}' invalid PowerToDrill {block.PowerToDrill}, set to 0"); block.PowerToDrill = 0; isValid = false; }
            if (block.Efficiency <= 0) { Debug.Error($"[GameSetLoader] Block '{block.ID}' invalid efficiency {block.Efficiency}, set to 1.0"); block.Efficiency = 1.0f; isValid = false; }
            if (block.DropQuantity <= 0) { Debug.Error($"[GameSetLoader] Block '{block.ID}' invalid DropQuantity {block.DropQuantity}, set to 1"); block.DropQuantity = 1; isValid = false; }

            return isValid;
        }

        private static BlockData CreateBlockData(BlockJSON block, string blockId)
        {
            bool hasLightColor = block.LightColor != Color3Byte.Black;
            var blockColor = hasLightColor ? block.LightColor.ToVector3() : Vector3.Zero;

            var blockData = new BlockData(block.Name, block.Type, block.IsTransparent, blockColor)
            {
                Id_string = blockId,
                Description = block.Description,
                Mass = (byte)Math.Clamp(block.Mass, 1, byte.MaxValue),
                Durability = (byte)Math.Clamp(block.Durability, 1, byte.MaxValue),
                PowerToDrill = (byte)Math.Clamp(block.PowerToDrill, 0, byte.MaxValue),
                Efficiency = Math.Max(0.1f, block.Efficiency),
                Category = block.Category,
                BaseFrontDirection = Block.GetDirectionFromNormal(block.FrontDirection)
            };

            blockData.Drop.Item.Id_string = string.IsNullOrWhiteSpace(block.Drop) ? "$self" : block.Drop;
            blockData.Drop.Count = (byte)Math.Clamp(block.DropQuantity, 1, byte.MaxValue);

            ApplyTextures(blockData, block, BlockState.Active);

            if (HasInactiveTextures(block))
            {
                ApplyTextures(blockData, block, BlockState.Inactive);
            }

            GiveBlockSounds(blockData, block);
            return blockData;
        }

        private static bool HasInactiveTextures(BlockJSON block)
        {
            return !string.IsNullOrEmpty(block.SidesOff) ||
                   !string.IsNullOrEmpty(block.UpOff) ||
                   !string.IsNullOrEmpty(block.ForwardOff);
        }

        private static void ApplyTextures(BlockData blockData, BlockJSON json, BlockState state)
        {
            string sides = state == BlockState.Active ? json.Sides : json.SidesOff;
            string up = state == BlockState.Active ? json.Up : json.UpOff;
            string down = state == BlockState.Active ? json.Down : json.DownOff;
            string left = state == BlockState.Active ? json.Left : json.LeftOff;
            string right = state == BlockState.Active ? json.Right : json.RightOff;
            string fwd = state == BlockState.Active ? json.Forward : json.ForwardOff;
            string back = state == BlockState.Active ? json.Back : json.BackOff;

            string defaultSide = string.IsNullOrEmpty(sides) && state == BlockState.Inactive ? json.Sides : sides;


            blockData.SetTexture(string.IsNullOrEmpty(up) ? sides : up, Direction.Up, state);
            blockData.SetTexture(string.IsNullOrEmpty(down) ? sides : down, Direction.Down, state);
            blockData.SetTexture(string.IsNullOrEmpty(left) ? sides : left, Direction.Left, state);
            blockData.SetTexture(string.IsNullOrEmpty(right) ? sides : right, Direction.Right, state);
            blockData.SetTexture(string.IsNullOrEmpty(fwd) ? sides : fwd, Direction.Forward, state);
            blockData.SetTexture(string.IsNullOrEmpty(back) ? sides : back, Direction.Back, state);
        }

        private static void GiveBlockSounds(BlockData blockData, BlockJSON modBlockData)
        {
            if (!GameAssets.Sounds.ContainsKey(modBlockData.SoundPlace))
            {
                blockData.SetDefaultPlaceSound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> wrong place sound: {modBlockData.SoundPlace}. Using default.");
            }
            else
            {
                blockData.SoundPlace = modBlockData.SoundPlace;
            }

            if (!GameAssets.Sounds.ContainsKey(modBlockData.SoundDestroy))
            {
                blockData.SetDefaultDestroySound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> wrong destroy sound: {modBlockData.SoundDestroy}. Using default.");
            }
            else
            {
                blockData.SoundDestroy = modBlockData.SoundDestroy;
            }
        }
    }
}