using Engine;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using System;
using System.Collections.Generic;

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

            if (block.Textures.Active == null) block.Textures.Active = new FaceTextures();
            if (block.Textures.Inactive == null) block.Textures.Inactive = new FaceTextures();
            if (block.Sounds == null) block.Sounds = new SoundGroup();

            block.Textures.Active.All = (block.Textures.Active.All ?? "").ToLower();
            block.Textures.Active.Up = (block.Textures.Active.Up ?? "").ToLower();
            block.Textures.Active.Down = (block.Textures.Active.Down ?? "").ToLower();
            block.Textures.Active.Left = (block.Textures.Active.Left ?? "").ToLower();
            block.Textures.Active.Right = (block.Textures.Active.Right ?? "").ToLower();
            block.Textures.Active.Forward = (block.Textures.Active.Forward ?? "").ToLower();
            block.Textures.Active.Back = (block.Textures.Active.Back ?? "").ToLower();

            block.Textures.Inactive.All = (block.Textures.Inactive.All ?? "").ToLower();
            block.Textures.Inactive.Up = (block.Textures.Inactive.Up ?? "").ToLower();
            block.Textures.Inactive.Down = (block.Textures.Inactive.Down ?? "").ToLower();
            block.Textures.Inactive.Left = (block.Textures.Inactive.Left ?? "").ToLower();
            block.Textures.Inactive.Right = (block.Textures.Inactive.Right ?? "").ToLower();
            block.Textures.Inactive.Forward = (block.Textures.Inactive.Forward ?? "").ToLower();
            block.Textures.Inactive.Back = (block.Textures.Inactive.Back ?? "").ToLower();

            block.Category = (block.Category ?? "").ToLower();
            block.Description = block.Description ?? "";
            block.Sounds.Place = block.Sounds.Place ?? "blockPlaceDefault";
            block.Sounds.Destroy = block.Sounds.Destroy ?? "blockDestroyDefault";
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
            return !string.IsNullOrEmpty(block.Textures.Inactive.All) ||
                   !string.IsNullOrEmpty(block.Textures.Inactive.Up) ||
                   !string.IsNullOrEmpty(block.Textures.Inactive.Down) ||
                   !string.IsNullOrEmpty(block.Textures.Inactive.Left) ||
                   !string.IsNullOrEmpty(block.Textures.Inactive.Right) ||
                   !string.IsNullOrEmpty(block.Textures.Inactive.Forward) ||
                   !string.IsNullOrEmpty(block.Textures.Inactive.Back);
        }

        private static void ApplyTextures(BlockData blockData, BlockJSON json, BlockState state)
        {
            FaceTextures currentFace = state == BlockState.Active ? json.Textures.Active : json.Textures.Inactive;

            string sides = currentFace.All;
            if (string.IsNullOrEmpty(sides) && state == BlockState.Inactive)
            {
                sides = json.Textures.Active.All;
            }

            string up = currentFace.Up;
            string down = currentFace.Down;
            string left = currentFace.Left;
            string right = currentFace.Right;
            string fwd = currentFace.Forward;
            string back = currentFace.Back;

            blockData.SetTexture(string.IsNullOrEmpty(up) ? sides : up, Direction.Up, state);
            blockData.SetTexture(string.IsNullOrEmpty(down) ? sides : down, Direction.Down, state);
            blockData.SetTexture(string.IsNullOrEmpty(left) ? sides : left, Direction.Left, state);
            blockData.SetTexture(string.IsNullOrEmpty(right) ? sides : right, Direction.Right, state);
            blockData.SetTexture(string.IsNullOrEmpty(fwd) ? sides : fwd, Direction.Forward, state);
            blockData.SetTexture(string.IsNullOrEmpty(back) ? sides : back, Direction.Back, state);
        }

        private static void GiveBlockSounds(BlockData blockData, BlockJSON modBlockData)
        {
            if (!GameAssets.Sounds.ContainsKey(modBlockData.Sounds.Place))
            {
                blockData.SetDefaultPlaceSound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> wrong place sound: {modBlockData.Sounds.Place}. Using default.");
            }
            else
            {
                blockData.SoundPlace = modBlockData.Sounds.Place;
            }

            if (!GameAssets.Sounds.ContainsKey(modBlockData.Sounds.Destroy))
            {
                blockData.SetDefaultDestroySound();
                Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> wrong destroy sound: {modBlockData.Sounds.Destroy}. Using default.");
            }
            else
            {
                blockData.SoundDestroy = modBlockData.Sounds.Destroy;
            }
        }
    }
}