using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Resource
{
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

        public string Sides { get; set; } = "";
        public string Up { get; set; } = "";
        public string Down { get; set; } = "";
        public string Left { get; set; } = "";
        public string Right { get; set; } = "";
        public string Forward { get; set; } = "";
        public string Back { get; set; } = "";

        public Vector2Byte WallsUVIndex = new Vector2Byte(0, 1);
        public Vector2Byte UpUVIndex = new Vector2Byte(0, 0);
        public Vector2Byte DownUVIndex = new Vector2Byte(0, 0);
        public Vector2Byte LeftUVIndex = new Vector2Byte(0, 0);
        public Vector2Byte RightUVIndex = new Vector2Byte(0, 0);
        public Vector2Byte ForwardUVIndex = new Vector2Byte(0, 0);
        public Vector2Byte BackUVIndex = new Vector2Byte(0, 0);

        public Vector2[] WallsUV;
        public Vector2[] UpUV;
        public Vector2[] DownUV;
        public Vector2[] LeftUV;
        public Vector2[] RightUV;
        public Vector2[] ForwardUV;
        public Vector2[] BackUV;

        public bool IsTransparent { get; set; } = false;
        public Vector3 LightColor { get; set; } = Vector3.Zero;

        public BlockItem AsItem;
        public Item Drop;
        public string DropIDFull { get; set; } = "$self";
        public short DropID { get; set; } = 0;
        public byte DropQuantity { get; set; } = 1;

        public string SoundPlace { get; set; } = "blockPlaceDefault";
        public string SoundDestroy { get; set; } = "blockDestroyDefault";

        public bool AllSidesAreSame = false;
        private Vector2[][] _uvFaceCache;

        public BlockData()
        {
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, bool isTransparent = false, Vector3? lightColor = null)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            UpUVIndex = textureCoords;
            DownUVIndex = textureCoords;
            LeftUVIndex = textureCoords;
            RightUVIndex = textureCoords;
            ForwardUVIndex = textureCoords;
            BackUVIndex = textureCoords;
            IsTransparent = isTransparent;
            if (lightColor.HasValue) LightColor = lightColor.Value;
            AllSidesAreSame = true;
        }

        public void CacheUVsByDirection()
        {
            if (AllSidesAreSame) return;

            _uvFaceCache = new Vector2[6][];
            _uvFaceCache[0] = DownUV ?? WallsUV;
            _uvFaceCache[1] = UpUV ?? WallsUV;
            _uvFaceCache[2] = LeftUV ?? WallsUV;
            _uvFaceCache[3] = RightUV ?? WallsUV;
            _uvFaceCache[4] = BackUV ?? WallsUV;
            _uvFaceCache[5] = ForwardUV ?? WallsUV;
        }

        public Vector2[] GetUvsByFaceAndDirection(Face face, Direction direction)
        {
            if (AllSidesAreSame)
                return WallsUV;

            byte remappedFace = BlockRotationTable.GetDirectionFaceRemap(direction, face);
            byte rotation = BlockRotationTable.GetDirectionUVRotation(direction, face);

            Vector2[] baseUV = _uvFaceCache[remappedFace];

            return ApplyRotation(baseUV, rotation);
        }

        public Vector2[] GetFaceUV(Face face)
        {
            if (AllSidesAreSame)
                return WallsUV;

            return _uvFaceCache != null ? _uvFaceCache[(byte)face] : GetFaceUVDirect(face);
        }

        private Vector2[] GetFaceUVDirect(Face face)
        {
            return face switch
            {
                Face.Down => DownUV ?? WallsUV,
                Face.Up => UpUV ?? WallsUV,
                Face.Left => LeftUV ?? WallsUV,
                Face.Right => RightUV ?? WallsUV,
                Face.Back => BackUV ?? WallsUV,
                Face.Forward => ForwardUV ?? WallsUV,
                _ => WallsUV
            };
        }

        private static Vector2[] ApplyRotation(Vector2[] uv, byte rotation)
        {
            return rotation switch
            {
                1 => RotateUV90Right(uv),
                2 => RotateUV180(uv),
                3 => RotateUV90Left(uv),
                _ => uv
            };
        }

        public static Vector2[] RotateByRotation(Vector2[] uvs, Rotation rot)
        {
            return rot switch
            {
                Rotation.Right => RotateUV90Right(uvs),
                Rotation.Down => RotateUV180(uvs),
                Rotation.Left => RotateUV90Left(uvs),
                _ => uvs
            };
        }

        public static Vector2[] RotateUV90Right(Vector2[] uvs)
        {
            return uvs.Length == 4 ? new[] { uvs[3], uvs[0], uvs[1], uvs[2] } : uvs;
        }

        public static Vector2[] RotateUV90Left(Vector2[] uvs)
        {
            return uvs.Length == 4 ? new[] { uvs[1], uvs[2], uvs[3], uvs[0] } : uvs;
        }

        public static Vector2[] RotateUV180(Vector2[] uvs)
        {
            return uvs.Length == 4 ? new[] { uvs[2], uvs[3], uvs[0], uvs[1] } : uvs;
        }

        public void SetDefaultPlaceSound() => SoundPlace = "blockPlaceDefault";
        public void SetDefaultDestroySound() => SoundDestroy = "blockDestroyDefault";
    }
}