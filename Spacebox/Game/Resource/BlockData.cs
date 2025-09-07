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
        private Vector2[][] _uvCache;

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

            SetFallbackUVs();

            _uvCache = new Vector2[36][];

            for (byte face = 0; face < 6; face++)
            {
                for (byte dir = 0; dir < 6; dir++)
                {
                    int index = face * 6 + dir;
                    _uvCache[index] = CalculateUVForFaceAndDirection((Face)face, (Direction)dir);
                }
            }
        }

        private void SetFallbackUVs()
        {
            if (LeftUV == null) LeftUV = WallsUV;
            if (RightUV == null) RightUV = WallsUV;
            if (ForwardUV == null) ForwardUV = WallsUV;
            if (BackUV == null) BackUV = WallsUV;
            if (UpUV == null) UpUV = WallsUV;
            if (DownUV == null) DownUV = WallsUV;
        }

        public Vector2[] GetUvsByFaceAndDirection(Face face, Direction direction)
        {
            if (AllSidesAreSame)
                return WallsUV;

            int index = (byte)face * 6 + (byte)direction;
            return _uvCache[index];
        }

        private Vector2[] CalculateUVForFaceAndDirection(Face face, Direction direction)
        {
            var baseUV = GetBaseUVForFace(face);
            return ApplyRotationForDirection(baseUV, face, direction);
        }

        private Vector2[] GetBaseUVForFace(Face face)
        {
            return face switch
            {
                Face.Up => UpUV,
                Face.Down => DownUV,
                Face.Left => LeftUV,
                Face.Right => RightUV,
                Face.Forward => ForwardUV,
                Face.Back => BackUV,
                _ => WallsUV
            };
        }

        private Vector2[] ApplyRotationForDirection(Vector2[] uv, Face face, Direction direction)
        {
            return direction switch
            {
                Direction.Up => uv,
                Direction.Down => GetDownRotationUV(uv, face),
                Direction.Left => GetLeftRotationUV(uv, face),
                Direction.Right => GetRightRotationUV(uv, face),
                Direction.Forward => GetForwardRotationUV(uv, face),
                Direction.Back => GetBackRotationUV(uv, face),
                _ => uv
            };
        }

        private Vector2[] GetDownRotationUV(Vector2[] uv, Face face)
        {
            return face switch
            {
                Face.Up => RotateUV180(DownUV),
                Face.Down => UpUV,
                Face.Left => RotateUV180(LeftUV),
                Face.Right => RotateUV180(RightUV),
                Face.Forward => RotateUV180(ForwardUV),
                Face.Back => RotateUV180(BackUV),
                _ => RotateUV180(uv)
            };
        }

        private Vector2[] GetLeftRotationUV(Vector2[] uv, Face face)
        {
            return face switch
            {
                Face.Up => RotateUV90Right(RightUV),
                Face.Down => RotateUV90Right(LeftUV),
                Face.Left => UpUV,
                Face.Right => DownUV,
                Face.Forward => RotateUV90Right(ForwardUV),
                Face.Back => RotateUV90Left(BackUV),
                _ => uv
            };
        }

        private Vector2[] GetRightRotationUV(Vector2[] uv, Face face)
        {
            return face switch
            {
                Face.Up => RotateUV90Left(LeftUV),
                Face.Down => RotateUV90Left(RightUV),
                Face.Left => DownUV,
                Face.Right => UpUV,
                Face.Forward => RotateUV90Left( ForwardUV),
                Face.Back => RotateUV90Right(BackUV),
                _ => uv
            };
        }

        private Vector2[] GetForwardRotationUV(Vector2[] uv, Face face)
        {
            return face switch
            {
                Face.Up => RotateUV180(BackUV),
                Face.Down => ForwardUV,
                Face.Left => RotateUV90Left(LeftUV),
                Face.Right => RotateUV90Right(RightUV),
                Face.Forward => UpUV,
                Face.Back => DownUV,
                _ => uv
            };
        }

        private Vector2[] GetBackRotationUV(Vector2[] uv, Face face)
        {
            return face switch
            {
                Face.Up => ForwardUV,
                Face.Down => RotateUV180(BackUV),
                Face.Left => RotateUV90Right(LeftUV),
                Face.Right => RotateUV90Left(RightUV),
                Face.Forward => DownUV,
                Face.Back => UpUV,
                _ => uv
            };
        }

        public static Vector2[] RotateByRotation(Vector2[] uvs, Rotation rot)
        {
            if(rot == Rotation.Up) return uvs;
            if (rot == Rotation.Down) return RotateUV180(uvs);
            if(rot == Rotation.Left) return RotateUV90Left(uvs);

            return RotateUV90Right(uvs);
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