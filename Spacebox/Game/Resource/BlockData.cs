using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
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

        public bool IsTransparent { get; private set; } = false;
        public Vector3 LightColor { get; private set; } = Vector3.Zero;
        public Direction BaseFrontDirection { get; set; } = Direction.Up;

        public BlockItem AsItem { get; set; }
        public ItemSlot Drop { get; private set; }

        public string SoundPlace { get; set; } = "blockPlaceDefault";
        public string SoundDestroy { get; set; } = "blockDestroyDefault";

        private class TextureState
        {
            public string[] TextureNames = new string[6];
            public Vector2[][] UVs = new Vector2[6][];
            public Vector2Byte[] UVIndices = new Vector2Byte[6];
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
            IsTransparent = baseBlock.IsTransparent;
            LightColor = baseBlock.LightColor;
            BaseFrontDirection = baseBlock.BaseFrontDirection;
            AsItem = baseBlock.AsItem;
            Drop = baseBlock.Drop;
            SoundPlace = baseBlock.SoundPlace;
            SoundDestroy = baseBlock.SoundDestroy;

            foreach (var state in baseBlock._texturesByState)
            {
                var newState = new TextureState
                {
                    AllSidesAreSame = state.Value.AllSidesAreSame
                };
                System.Array.Copy(state.Value.TextureNames, newState.TextureNames, 6);
                System.Array.Copy(state.Value.UVs, newState.UVs, 6);
                System.Array.Copy(state.Value.UVIndices, newState.UVIndices, 6);
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

                    state.UVs[i] = GameAssets.AtlasBlocks.GetUVByName(textureName);
                    state.UVIndices[i] = GameAssets.AtlasBlocks.GetUVIndexByName(textureName);
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

        public Vector2Byte GetFaceUVIndex(Direction direction, BlockState state = BlockState.Active)
        {
            if (!_texturesByState.TryGetValue(state, out var textureState))
            {
                textureState = _texturesByState[BlockState.Active];
            }
            return textureState.UVIndices[(int)direction];
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