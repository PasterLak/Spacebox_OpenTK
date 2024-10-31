using Spacebox.Common;
using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public class Chunk
    {
        public const sbyte Size = 16;

        public Vector3 Position { get; private set; }
        public Block[,,] Blocks { get; private set; }

        private Mesh _mesh;

        public Chunk(Vector3 position)
        {
            Position = position;
            Blocks = new Block[Size, Size, Size];
        
            // GenerateBlocks(); 
            GenerateSphereBlocks(); 
            GenerateMesh();
        }

        private void GenerateBlocks()
        {
            Random r = new Random();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        float xx = (float)r.NextDouble() * 5;
                     
                        Vector2 textureCoords = new Vector2(xx, 0); 
                        Vector3 color = new Vector3(1f, 1f, 1f);

                        Blocks[x, y, z] = xx < 1 ? new Block(BlockType.Air, textureCoords, color)
                                                 : new Block(BlockType.Solid, textureCoords, color);
                    }
        }

        private void GenerateSphereBlocks()
        {
            Vector3 center = new Vector3(Size / 2f, Size / 2f, Size / 2f);
            float radius = Size / 2f;

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Vector3 position = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        float distance = Vector3.Distance(position, center);

                        if (distance <= radius)
                        {
                            
                            Random random = new Random();

                            var r = random.Next(0, 10);

                            Vector2 textureCoords = Vector2.Zero;

                            if (r < 8) textureCoords = new Vector2(1, 2);
                            if (r == 8) textureCoords = new Vector2(2, 2);
                            if (r == 9)
                            {
                                textureCoords = new Vector2(3, 2);

                            }

                            Vector3 color = new Vector3(1f, 1f, 1f); // Белый цвет
                            Blocks[x, y, z] = new Block(BlockType.Solid, textureCoords, color);
                            if (r == 9)
                            {
                                Blocks[x, y, z].IsTransparent = true;

                            }

                        }
                        else
                        {
                            // Вне сферы
                            Blocks[x, y, z] = new Block(BlockType.Air, new Vector2(0, 0));
                        }
                    }
        }

        public void GenerateMesh()
        {
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint index = 0;

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Block block = Blocks[x, y, z];
                        if (block.Type == BlockType.Air)
                            continue;

                        if (IsTransparentBlock(x - 1, y, z))
                            AddFace(vertices, indices, x, y, z, Face.Left, block, ref index);
                        if (IsTransparentBlock(x + 1, y, z))
                            AddFace(vertices, indices, x, y, z, Face.Right, block, ref index);
                        if (IsTransparentBlock(x, y - 1, z))
                            AddFace(vertices, indices, x, y, z, Face.Bottom, block, ref index);
                        if (IsTransparentBlock(x, y + 1, z))
                            AddFace(vertices, indices, x, y, z, Face.Top, block, ref index);
                        if (IsTransparentBlock(x, y, z - 1))
                            AddFace(vertices, indices, x, y, z, Face.Back, block, ref index);
                        if (IsTransparentBlock(x, y, z + 1))
                            AddFace(vertices, indices, x, y, z, Face.Front, block, ref index);
                    }

            _mesh = new Mesh(vertices.ToArray(), indices.ToArray());
        }

        private bool IsTransparentBlock(int x, int y, int z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return true;

            if(Blocks[x, y, z].IsAir()) return true;

            return Blocks[x, y, z].IsTransparent;
        }

        private void AddFace(List<float> vertices, List<uint> indices, int x, int y, int z, Face face, Block block, ref uint index)
        {
            Vector3[] faceVertices = CubeMeshData.GetFaceVertices(face);
            Vector2[] faceUVs = UVAtlas.GetUVs((int)block.TextureCoords.X, (int)block.TextureCoords.Y);

            for (int i = 0; i < faceVertices.Length; i++)
            {
                Vector3 vertex = faceVertices[i];
                vertices.Add(vertex.X + x);
                vertices.Add(vertex.Y + y);
                vertices.Add(vertex.Z + z);

                vertices.Add(faceUVs[i].X);
                vertices.Add(faceUVs[i].Y);

                vertices.Add(block.Color.X);
                vertices.Add(block.Color.Y);
                vertices.Add(block.Color.Z);
            }

            uint[] faceIndices = { 0, 1, 2, 2, 3, 0 };
            for (int i = 0; i < faceIndices.Length; i++)
            {
                indices.Add(index + faceIndices[i]);
            }

            index += 4;
        }

        public void Shift(Vector3 shift)
        {

            Position -= shift;
        }

        public void Draw(Shader shader)
        {
            Matrix4 model = Matrix4.CreateTranslation(Position);
            shader.SetMatrix4("model", model);

            _mesh.Draw(shader);
        }

        public void SetBlock(int x, int y, int z, Block block)
        {
            if (IsInRange(x, y, z))
            {
                Blocks[x, y, z] = block;
                GenerateMesh();
            }
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
                return Blocks[x, y, z];
            else
                return new Block(BlockType.Air, new Vector2(0, 0));
        }

        public void RemoveBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
            {
                Blocks[x, y, z] = new Block(BlockType.Air, new Vector2(0, 0));
                GenerateMesh();
            }
        }

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}
