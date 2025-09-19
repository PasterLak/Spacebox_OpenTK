using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Resource
{
    public struct ID
    {
        public short intern; 
        public string str;
    }
    public class BlockJSON
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

        private readonly string[] _faceTextures = new string[7];
        private readonly Vector2Byte[] _faceUVIndices = new Vector2Byte[7];
        private readonly Vector2[][] _faceUVs = new Vector2[7][];

        public bool IsTransparent { get; private set; } = false;
        public Vector3 LightColor { get; private set; } = Vector3.Zero;
        public Direction BaseFrontDirection { get; private set; } = Direction.Up;
       

        public BlockItem AsItem;
        public ItemSlot Drop;

        public string SoundPlace { get; set; } = "blockPlaceDefault";
        public string SoundDestroy { get; set; } = "blockDestroyDefault";

        public bool AllSidesAreSame { get; private set; } = true;

        public string GetFaceTexture(Direction direction) => _faceTextures[(int)direction];
        public void SetFaceTexture(Direction direction, string texture) => _faceTextures[(int)direction] = texture;

        public string Sides
        {
            get => _faceTextures[6];
            set => _faceTextures[6] = value;
        }

        public Vector2Byte GetFaceUVIndex(Direction direction) => _faceUVIndices[(int)direction];
        public void SetFaceUVIndex(Direction direction, Vector2Byte uvIndex) => _faceUVIndices[(int)direction] = uvIndex;

        public Vector2Byte WallsUVIndex
        {
            get => _faceUVIndices[6];
            set => _faceUVIndices[6] = value;
        }

        public Vector2[] GetFaceUVDirect(Direction direction) => _faceUVs[(int)direction];
        public void SetFaceUV(Direction direction, Vector2[] uv) => _faceUVs[(int)direction] = uv;

        public Vector2[] WallsUV
        {
            get => _faceUVs[6];
            set => _faceUVs[6] = value;
        }

        public BlockJSON(string name, string type, Vector2Byte textureCoords, bool isTransparent = false, Vector3? lightColor = null)
        {
            Drop = new ItemSlot(null, 0, 0);
            Drop.Count = 0;
            Name = name;
            Type = type;
            IsTransparent = isTransparent;
            if (lightColor.HasValue) LightColor = lightColor.Value;

            for (int i = 0; i < 7; i++)
            {
                _faceUVIndices[i] = textureCoords;
            }

            AllSidesAreSame = true;
        }

        public static void CacheUvs(BlockJSON b)
        {
            b.WallsUV = GameAssets.AtlasBlocks.GetUVByName(b.Sides);
            b.WallsUVIndex = GameAssets.AtlasBlocks.GetUVIndexByName(b.Sides);

            for (int i = 0; i < 6; i++)
            {
                var direction = (Direction)i;
                var textureName = b.GetFaceTexture(direction);

                b.SetFaceUV(direction, GameAssets.AtlasBlocks.GetUVByName(textureName));
                b.SetFaceUVIndex(direction, GameAssets.AtlasBlocks.GetUVIndexByName(textureName));
            }

            bool allWallsSame = true;
            for (int i = 2; i < 6; i++)
            {
                if (b.GetFaceTexture((Direction)i) != b.Sides)
                {
                    allWallsSame = false;
                    break;
                }
            }

            if (allWallsSame)
            {
                for (int i = 2; i < 6; i++)
                {
                    var direction = (Direction)i;
                    b.SetFaceUV(direction, b.WallsUV);
                    b.SetFaceUVIndex(direction, b.WallsUVIndex);
                }
            }

            if (b.GetFaceTexture(Direction.Up) == b.GetFaceTexture(Direction.Down))
            {
                b.SetFaceUV(Direction.Down, b.GetFaceUVDirect(Direction.Up));
                b.SetFaceUVIndex(Direction.Down, b.GetFaceUVIndex(Direction.Up));
            }

            bool allSameTexture = true;
            for (int i = 0; i < 6; i++)
            {
                if (b.GetFaceTexture((Direction)i) != b.Sides)
                {
                    allSameTexture = false;
                    break;
                }
            }

            b.AllSidesAreSame = allSameTexture;
        }

        public Vector2[] GetFaceUV(Face face)
        {
            if (AllSidesAreSame)
                return WallsUV;

            var direction = FaceToDirection(face);
            return GetFaceUVDirect(direction) ?? WallsUV;
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