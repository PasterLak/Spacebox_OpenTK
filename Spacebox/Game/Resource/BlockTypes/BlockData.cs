using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Generation.Tools;
using System.Collections.Generic;

namespace Spacebox.Game.Resource
{
    public enum BlockState
    {
        Active,
        Inactive
    }

    public struct ID
    {
        public short intern;
        public string str;
    }

    public class BlockData
    {
        public short Id;
        public string Id_string;
        public string Name;
        public string Description;
        public string Type;
        public string Category;
        public byte Mass = 1;
        public byte PowerToDrill = 0;
        public byte Durability = 0;
        public float Efficiency = 1f;
        public ushort ProcessingPower;
        public BlockComponentDefinition[] Components;

        public bool IsTransparent { get; private set; } = false;
        public Vector3 LightColor { get; private set; } = Vector3.Zero;
        public Direction BaseFrontDirection { get; set; } = Block.DefaultDirection;

        public BlockItem AsItem { get; set; }
        public ItemSlot Drop { get; private set; }

        public string SoundPlace { get; set; } = "blockPlaceDefault";
        public string SoundDestroy { get; set; } = "blockDestroyDefault";
        public string[] FootstepSounds { get; set; } = Array.Empty<string>();
        public string WorkingSound { get; set; } = "";
        public string FinishedSound { get; set; } = "";

        private class TextureState
        {
            public string[] TextureNames = new string[6];
            public Vector2[][] UVs = new Vector2[6][];
            public string[] ResolvedTextureNames = new string[6];
            public Vector2i[] Tiles = { Vector2i.One, Vector2i.One, Vector2i.One, Vector2i.One, Vector2i.One, Vector2i.One };
            public bool AllSidesAreSame = true;
        }

        private readonly Dictionary<BlockState, TextureState> _texturesByState = new();



        public BlockData(string name, string type, bool isTransparent = false, Vector3? lightColor = null)
        {
            Name = name;
            Type = type;
            IsTransparent = isTransparent;
            if (lightColor.HasValue) LightColor = lightColor.Value;

            Drop = new ItemSlot(null, 0, 0) { Count = 0 };
            _texturesByState[BlockState.Active] = new TextureState();
        }

    

        public Vector3Byte Size { get; set; } = new Vector3Byte(1);
        public int CellPixels { get; set; }
        public bool TextureFit { get; set; }
        public bool Unbreakable { get; set; }
        public BlockShape Shape { get; set; }
        public bool IsMultiblock => Size.X > 1 || Size.Y > 1 || Size.Z > 1;
        public int CellCount => Size.X * Size.Y * Size.Z;

        public Vector2i GetFaceTiles(Face face, BlockState state = BlockState.Active)
        {
            if (!_texturesByState.TryGetValue(state, out var textureState) &&
                !_texturesByState.TryGetValue(BlockState.Active, out textureState))
                return Vector2i.One;

            return textureState.Tiles[(int)FaceToDirection(face)];
        }

        public bool AllSidesAreSame(BlockState state = BlockState.Active)
        {
            if (_texturesByState.TryGetValue(state, out var textureState))
            {
                return textureState.AllSidesAreSame;
            }
            return true;
        }

        public BlockData(BlockData baseBlock)
        {
            Id = baseBlock.Id;
            Id_string = baseBlock.Id_string;
            Name = baseBlock.Name;
            Description = baseBlock.Description;
            Type = baseBlock.Type;
            Category = baseBlock.Category;
            Mass = baseBlock.Mass;
            PowerToDrill = baseBlock.PowerToDrill;
            Durability = baseBlock.Durability;
            Efficiency = baseBlock.Efficiency;
            ProcessingPower = baseBlock.ProcessingPower;
            Components = baseBlock.Components;
            IsTransparent = baseBlock.IsTransparent;
            LightColor = baseBlock.LightColor;
            BaseFrontDirection = baseBlock.BaseFrontDirection;
            Size = baseBlock.Size;
            CellPixels = baseBlock.CellPixels;
            TextureFit = baseBlock.TextureFit;
            Unbreakable = baseBlock.Unbreakable;
            Shape = baseBlock.Shape;
            AsItem = baseBlock.AsItem;
            Drop = baseBlock.Drop;
            SoundPlace = baseBlock.SoundPlace;
            SoundDestroy = baseBlock.SoundDestroy;
            FootstepSounds = baseBlock.FootstepSounds;
            WorkingSound = baseBlock.WorkingSound;
            FinishedSound = baseBlock.FinishedSound;

            foreach (var state in baseBlock._texturesByState)
            {
                var newState = new TextureState
                {
                    AllSidesAreSame = state.Value.AllSidesAreSame
                };
                System.Array.Copy(state.Value.TextureNames, newState.TextureNames, 6);
                System.Array.Copy(state.Value.UVs, newState.UVs, 6);
                System.Array.Copy(state.Value.ResolvedTextureNames, newState.ResolvedTextureNames, 6);
                System.Array.Copy(state.Value.Tiles, newState.Tiles, 6);
                _texturesByState[state.Key] = newState;
            }
        }

        public void SetTexture(string textureName, Direction face = Direction.Up, BlockState state = BlockState.Active)
        {
            if (!_texturesByState.ContainsKey(state))
                _texturesByState[state] = new TextureState();

            var textureState = _texturesByState[state];
            textureState.TextureNames[(int)face] = textureName;
        }

        public void SetTextureAllSides(string textureName, BlockState state = BlockState.Active)
        {
            if (!_texturesByState.ContainsKey(state))
                _texturesByState[state] = new TextureState();

            var textureState = _texturesByState[state];
            for (int i = 0; i < 6; i++)
            {
                textureState.TextureNames[i] = textureName;
            }
        }

        public void SetTextureSide(string textureName, BlockState state = BlockState.Active)
        {
            SetTexture(textureName, Direction.Left, state);
            SetTexture(textureName, Direction.Right, state);
            SetTexture(textureName, Direction.Forward, state);
            SetTexture(textureName, Direction.Back, state);
        }

        public static void CacheUvs(BlockData b)
        {
            var faces = new List<FaceTexture>(12);
            foreach (var kvp in b._texturesByState)
            {
                var state = kvp.Value;
                state.AllSidesAreSame = true;
                string firstTexture = state.TextureNames[0];

                for (int i = 0; i < 6; i++)
                {
                    string textureName = state.TextureNames[i];

                    if (string.IsNullOrEmpty(textureName))
                    {
                        if (kvp.Key != BlockState.Active && b._texturesByState.ContainsKey(BlockState.Active))
                        {
                            textureName = b._texturesByState[BlockState.Active].TextureNames[i];
                        }
                        else
                        {
                            textureName = firstTexture;
                        }
                    }

                    if (textureName != firstTexture)
                        state.AllSidesAreSame = false;

                    var region = GameAssets.AtlasBlocks.GetRegion(textureName);
                    state.UVs[i] = region?.Uvs;
                    state.ResolvedTextureNames[i] = textureName;
                    if (region == null) continue;

                    MultiblockLayout.GetFaceCells(b.Size, (Direction)i, out int columns, out int rows);
                    faces.Add(new FaceTexture(region.Width, region.Height, columns, rows));
                }
            }

            int cellPixels = TextureTiling.ResolveCellPixels(faces, b.CellPixels);
            foreach (var state in b._texturesByState.Values)
            {
                for (int i = 0; i < 6; i++)
                {
                    var region = GameAssets.AtlasBlocks.GetRegion(state.ResolvedTextureNames[i]);
                    MultiblockLayout.GetFaceCells(b.Size, (Direction)i, out int columns, out int rows);
                    state.Tiles[i] = region == null ? Vector2i.One : TextureTiling.GetFaceTiles(new FaceTexture(region.Width, region.Height, columns, rows), cellPixels, b.TextureFit);
                }
            }
        }

        public Vector2[] GetFaceUV(Face face, BlockState state = BlockState.Active)
        {
            if (!_texturesByState.TryGetValue(state, out var textureState))
            {
                if (state != BlockState.Active && _texturesByState.TryGetValue(BlockState.Active, out var activeState))
                    textureState = activeState;
                else
                    return null;
            }

            int dirIndex = (int)FaceToDirection(face);

            if (textureState.AllSidesAreSame)
                return textureState.UVs[0];

            return textureState.UVs[dirIndex];
        }

        public string GetFaceTextureName(Direction direction, BlockState state = BlockState.Active)
        {
            if (!_texturesByState.TryGetValue(state, out var textureState))
            {
                textureState = _texturesByState[BlockState.Active];
            }
            return textureState.ResolvedTextureNames[(int)direction];
        }

        private static Direction FaceToDirection(Face face)
        {
            return face switch
            {
                Face.Down => Direction.Down,
                Face.Up => Direction.Up,
                Face.Left => Direction.Left,
                Face.Right => Direction.Right,
                Face.Back => Direction.Back,
                Face.Forward => Direction.Forward,
                _ => Direction.Up
            };
        }

        public void SetDefaultPlaceSound() => SoundPlace = "blockPlaceDefault";
        public void SetDefaultDestroySound() => SoundDestroy = "blockDestroyDefault";
    }
}