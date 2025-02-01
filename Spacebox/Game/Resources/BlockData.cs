using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Engine;
namespace Spacebox.Game.Resources
{
    public class BlockData
    {
        public short Id;
        public string Name;
        public string Type;
        public string Category;
        public byte Mass = 1;  // 1 is minimum
        public byte PowerToDrill = 0; // 0 can destroy every drill
        public byte Durability = 0; // instantly destroy
        public float Efficiency = 1f;
        public string Sides { get; set; } = "";
        public string Top { get; set; } = "";
        public string Bottom { get; set; } = "";

        public Vector2Byte WallsUVIndex = new Vector2Byte(0, 1);
        public Vector2Byte TopUVIndex = new Vector2Byte(0, 0);
        public Vector2Byte BottomUVIndex = new Vector2Byte(0, 0);

        public Vector2[] WallsUV;
        public Vector2[] TopUV;
        public Vector2[] BottomUV;
        public bool IsTransparent { get; set; } = false;
        public Vector3 LightColor { get; set; } = new Vector3(0, 0, 0);

        public BlockItem Item;

        public string SoundPlace { get; set; } = "blockPlaceDefault";
        public string SoundDestroy { get; set; } = "blockDestroyDefault";

        public bool AllSidesAreSame = false;

        public void SetDefaultPlaceSound()
        {
            SoundPlace = "blockPlaceDefault";
        }
        public void SetDefaultDestroySound()
        {
            SoundDestroy = "blockDestroyDefault";
        }

        private Dictionary<Direction, Vector2[]> TopUvsByDirection;
        private Dictionary<Direction, Vector2[]> BottomUvsByDirection;
        private Dictionary<Direction, Vector2[]> LeftUvsByDirection;
        private Dictionary<Direction, Vector2[]> RightUvsByDirection;
        private Dictionary<Direction, Vector2[]> FrontUvsByDirection;
        private Dictionary<Direction, Vector2[]> BackUvsByDirection;

        public BlockData(string name, string type, Vector2Byte textureCoords)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = textureCoords;
            BottomUVIndex = textureCoords;

            AllSidesAreSame = true;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, bool isTransparent)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = textureCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
            AllSidesAreSame = true;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, bool isTransparent, Vector3 color)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = textureCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
            LightColor = color;
            AllSidesAreSame = true;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, Vector2Byte topCoords)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = textureCoords;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, Vector2Byte topCoords, bool isTransparent)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, Vector2Byte topCoords, bool isTransparent, Vector3 color)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = textureCoords;

            IsTransparent = isTransparent;
            LightColor = color;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, Vector2Byte topCoords, Vector2Byte bottomCoords)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = bottomCoords;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, Vector2Byte topCoords, Vector2Byte bottomCoords, bool isTransparent)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = bottomCoords;

            IsTransparent = isTransparent;
        }

        public BlockData(string name, string type, Vector2Byte textureCoords, Vector2Byte topCoords, Vector2Byte bottomCoords, bool isTransparent, Vector3 color)
        {
            Name = name;
            Type = type;
            WallsUVIndex = textureCoords;
            TopUVIndex = topCoords;
            BottomUVIndex = bottomCoords;

            IsTransparent = isTransparent;
            LightColor = color;
        }


        public Vector2[] GetUvsByFaceAndDirection(Face face, Direction direction)
        {
            if (AllSidesAreSame)
            {
                return WallsUV;
            }

            if (face == Face.Bottom)
            {
                return BottomUvsByDirection[direction];
            }
            else if (face == Face.Left)
            {
                return LeftUvsByDirection[direction];
            }
            else if (face == Face.Right)
            {
                return RightUvsByDirection[direction];
            }
            else if (face == Face.Front)
            {
                return FrontUvsByDirection[direction];
            }
            else if (face == Face.Back)
            {
                return BackUvsByDirection[direction];
            }
            else
            {
                return TopUvsByDirection[direction];
            }
        }

        public void CacheUVsByDirection()
        {
            if (AllSidesAreSame) return;

            TopUvsByDirection = new Dictionary<Direction, Vector2[]>();
            BottomUvsByDirection = new Dictionary<Direction, Vector2[]>();

            LeftUvsByDirection = new Dictionary<Direction, Vector2[]>();
            RightUvsByDirection = new Dictionary<Direction, Vector2[]>();
            FrontUvsByDirection = new Dictionary<Direction, Vector2[]>();
            BackUvsByDirection = new Dictionary<Direction, Vector2[]>();

            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                if (dir == Direction.Up)
                {
                    TopUvsByDirection.Add(dir, TopUV);
                    BottomUvsByDirection.Add(dir, BottomUV);

                    LeftUvsByDirection.Add(dir, WallsUV);
                    RightUvsByDirection.Add(dir, WallsUV);
                    FrontUvsByDirection.Add(dir, WallsUV);
                    BackUvsByDirection.Add(dir, WallsUV);
                }
                else if (dir == Direction.Down)
                {
                    TopUvsByDirection.Add(dir, Rotate180(BottomUV));
                    BottomUvsByDirection.Add(dir, TopUV);

                    LeftUvsByDirection.Add(dir, Rotate180(WallsUV));
                    RightUvsByDirection.Add(dir, Rotate180(WallsUV));
                    FrontUvsByDirection.Add(dir, Rotate180(WallsUV));
                    BackUvsByDirection.Add(dir, Rotate180(WallsUV));
                }
                else if (dir == Direction.Left)
                {
                    TopUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                    BottomUvsByDirection.Add(dir, Rotate90Right(WallsUV));

                    LeftUvsByDirection.Add(dir, TopUV);
                    RightUvsByDirection.Add(dir, BottomUV);
                    FrontUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                    BackUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                }
                else if (dir == Direction.Right)
                {
                    TopUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                    BottomUvsByDirection.Add(dir, Rotate90Left(WallsUV));

                    LeftUvsByDirection.Add(dir, BottomUV);
                    RightUvsByDirection.Add(dir, TopUV);
                    FrontUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                    BackUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                }
                else if (dir == Direction.Forward)
                {
                    TopUvsByDirection.Add(dir, Rotate180(WallsUV));
                    BottomUvsByDirection.Add(dir, WallsUV);

                    LeftUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                    RightUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                    FrontUvsByDirection.Add(dir, TopUV);
                    BackUvsByDirection.Add(dir, BottomUV);
                }
                else
                {
                    TopUvsByDirection.Add(dir, WallsUV);
                    BottomUvsByDirection.Add(dir, Rotate180(WallsUV));

                    LeftUvsByDirection.Add(dir, Rotate90Right(WallsUV));
                    RightUvsByDirection.Add(dir, Rotate90Left(WallsUV));
                    FrontUvsByDirection.Add(dir, BottomUV);
                    BackUvsByDirection.Add(dir, TopUV);
                }

            }
        }

        public Vector2[] Rotate90Right(Vector2[] uvs)
        {
            if (uvs.Length != 4)
                return WallsUV;

            return new Vector2[]
            {
                uvs[3],
                uvs[0],
                uvs[1],
                uvs[2]
            };
        }

        public Vector2[] Rotate90Left(Vector2[] uvs)
        {
            if (uvs.Length != 4)
                return WallsUV;

            return new Vector2[]
            {
                uvs[1],
                uvs[2],
                uvs[3],
                uvs[0]
            };
        }

        public Vector2[] Rotate180(Vector2[] uvs)
        {
            if (uvs.Length != 4)
                return WallsUV;

            return new Vector2[]
            {
                uvs[2],
                uvs[3],
                uvs[0],
                uvs[1]
            };
        }

    }
}
