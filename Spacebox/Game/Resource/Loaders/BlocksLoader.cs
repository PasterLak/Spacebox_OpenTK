using Engine;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Resource.Content;
using System.Text.Json;
using System;
using System.Collections.Generic;

namespace Spacebox.Game.Resource
{
    public static class BlocksLoader
    {
        public const int MaxProcessingPower = 8000;

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

        public static void LoadBlocks(GameSetContent content, string[] modelSearchRoots)
        {
            if (!GameAssets.HasItem("default:void")) AddVoidBlock();
            if (!GameAssets.HasItem("default:air")) AddAirBlock();

            try
            {
                var shapes = new BlockShapeResolver(modelSearchRoots);

                foreach (var entry in content.Blocks)
                {
                    var block = Deserialize(entry);
                    if (block == null) continue;
                    if (!ProcessBlock(block, shapes, entry.Namespace))
                        continue;
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Error loading blocks: {ex.Message}");
            }
        }

        private static BlockJSON Deserialize(ContentEntry entry)
        {
            try
            {
                return entry.Data.Deserialize<BlockJSON>(ContentJson.CaseInsensitive);
            }
            catch (JsonException ex)
            {
                Debug.Error($"[GameSetLoader] Block '{entry.Key}' in {entry.Source} could not be read: {ex.Message}");
                return null;
            }
        }

        private static bool ProcessBlock(BlockJSON blockJson, BlockShapeResolver shapes, string ns)
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
            FixBlockValues(blockJson);

            var (blockNamespace, shortId) = GameSetLoader.DeclaredId(blockJson.ID, ns);
            var blockId = GameSetLoader.ValidateIdString(blockNamespace, shortId);
            blockId = GameSetLoader.CombineId(blockNamespace, blockId);

            if (GameAssets.HasItem(blockId))
            {
                Debug.Error($"[GameSetLoader] Block '{blockId}' already exists and was skipped");
                return false;
            }

            try
            {
                BlockData blockData = CreateBlockData(blockJson, blockId);
                blockData.Shape = shapes.Resolve(blockJson.Model);

                if (blockData.Type == "storage")
                {
                    var Size = blockJson.StorageSize;
                    if (Size.X < 1) Size = new Vector2Byte((byte)1, Size.Y);
                    if (Size.Y < 1) Size = new Vector2Byte(Size.X, (byte)1);

                    blockData = new StorageBlockData(blockData, Size);
                }
                else if (blockData.Type == "claim")
                {
                    blockData = new ClaimBeaconBlockData(blockData);
                }
                else if (blockData.Type == "gravity")
                {
                    blockData = new GravityGeneratorBlockData(blockData, blockJson.Gravity);
                }
                else if (blockData.Type == "generator")
                {
                    blockData = new GeneratorBlockData(blockData, blockJson.Generator, blockJson.Explosive);
                }
                else if (blockData.Type == "door")
                {
                    blockData = new DoorBlockData(blockData, blockJson.Door);
                }
                else if (blockData.Type == "explosive")
                {
                    blockData = new ExplosiveBlockData(blockData, blockJson.Explosive);
                }
                else if (blockData.Type == "pipe")
                {
                    blockData = new PipeBlockData(blockData, blockJson.Pipe);
                }
                else if (blockData.Type == "dnareconstructor")
                {
                    blockData = new DnaReconstructorBlockData(blockData, blockJson.DnaReconstructor, GameSetLoader.ModInfo.ModId);
                }
                else if (blockData.Type == "thruster")
                {
                    blockData = new ThrusterBlockData(blockData, blockJson.Thruster);
                }
                else if (blockData.Type == "shipconsole")
                {
                    blockData = new ShipConsoleBlockData(blockData, blockJson.Console);
                }
                else if (blockData.Type == "cannon")
                {
                    blockData = new CannonBlockData(blockData, blockJson.Cannon);
                }
                else if (blockData.Type == "shipcamera")
                {
                    blockData = new ShipCameraBlockData(blockData, blockJson.ShipCamera);
                }
                else if (blockData.Type == "disassembler")
                {
                    blockData = new DisassemblerBlockData(blockData, blockJson.Disassembler);
                }
                else if (blockData.Type == "sign")
                {
                    blockData.Shape ??= shapes.Resolve(SignPlateGeometry.Name);
                    blockData.Size = new Vector3Byte(Math.Clamp(blockJson.Sign?.Width ?? 1, 1, SignBlockData.MaxWidth), 1, 1);
                    blockData.BaseFrontDirection = Direction.Up;
                    blockData = new SignBlockData(blockData, blockJson.Sign);
                }
                else if (blockData.Type == "cable")
                {
                    blockData = new CableBlockData(blockData, blockJson.Cable);
                }
                else if (blockData.Type == "frame")
                {
                    blockData.Shape ??= PictureFrameGeometry.Shape;
                    blockData.Size = new Vector3Byte(PictureFrameBlockData.ClampSize(blockJson.Frame?.Width ?? 1), 1, PictureFrameBlockData.ClampSize(blockJson.Frame?.Height ?? 1));
                    blockData.BaseFrontDirection = Direction.Up;
                    blockData = new PictureFrameBlockData(blockData, blockJson.Frame, GameSetLoader.ModInfo.ModId);
                }
                else if (blockData.Type == "oxygengenerator")
                {
                    blockData = new OxygenGeneratorBlockData(blockData, blockJson.OxygenGenerator, GameSetLoader.ModInfo.ModId);
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

        private static void FixBlockValues(BlockJSON block)
        {
            if (block.Mass <= 0) { Debug.Warning($"[GameSetLoader] Block '{block.ID}' invalid mass {block.Mass}, set to 1"); block.Mass = 1; }
            if (block.Durability <= 0) { Debug.Warning($"[GameSetLoader] Block '{block.ID}' invalid durability {block.Durability}, set to 1"); block.Durability = 1; }
            if (block.PowerToDrill < 0) { Debug.Warning($"[GameSetLoader] Block '{block.ID}' invalid PowerToDrill {block.PowerToDrill}, set to 0"); block.PowerToDrill = 0; }
            if (block.Efficiency <= 0) { Debug.Warning($"[GameSetLoader] Block '{block.ID}' invalid efficiency {block.Efficiency}, set to 1.0"); block.Efficiency = 1.0f; }
            if (block.DropQuantity <= 0) { Debug.Warning($"[GameSetLoader] Block '{block.ID}' invalid DropQuantity {block.DropQuantity}, set to 1"); block.DropQuantity = 1; }
        }

        private static byte ClampSize(byte value)
        {
            return (byte)Math.Clamp((int)value, 1, MultiblockLayout.MaxSize);
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
                ProcessingPower = (ushort)Math.Clamp(block.Processing?.Power ?? 0, 0, MaxProcessingPower),
                Category = block.Category,
                BaseFrontDirection = Block.GetDirectionFromNormal(block.FrontDirection),
                Size = new Vector3Byte(ClampSize(block.Size.X), ClampSize(block.Size.Y), ClampSize(block.Size.Z)),
                CellPixels = Math.Clamp(block.CellPixels, 0, TextureTiling.MaxCellPixels),
                TextureFit = block.TextureFit,
                Unbreakable = block.Unbreakable
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

            blockData.FootstepSounds = Spacebox.Game.Player.Footsteps.FootstepSoundNames.KeepExisting(modBlockData.Sounds.Footsteps, GameAssets.Sounds.ContainsKey,
                name => Debug.Error($"[GamesetLoader] Block <{modBlockData.Name}> wrong footstep sound: {name}. Skipped."));
            blockData.WorkingSound = OptionalSound(modBlockData.Sounds.Working, modBlockData.Name, "working");
            blockData.FinishedSound = OptionalSound(modBlockData.Sounds.Finished, modBlockData.Name, "finished");
        }

        private static string OptionalSound(string name, string blockName, string kind)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (GameAssets.Sounds.ContainsKey(name)) return name;
            Debug.Error($"[GamesetLoader] Block <{blockName}> wrong {kind} sound: {name}. Skipped.");
            return "";
        }
    }
}