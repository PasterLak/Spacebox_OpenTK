using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;
using OpenTK.Graphics.OpenGL4;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Generation
{
    public class SpaceEntity : Node3D, IDisposable
    {
        public const byte SizeChunks = 4; // will be 16
        public const byte SizeChunksHalf = SizeChunks / 2;
        public const short SizeBlocks = SizeChunks * Chunk.Size;
        public const short SizeBlocksHalf = SizeChunks * Chunk.Size / 2;

        public int EntityID { get; private set; } = 0;
        public ulong Mass { get; set; } = 0;

        private readonly Octree<Chunk> octree;
        public BoundingBox BoundingBox { get; private set; }
        public Vector3 PositionWorld { get; private set; }

        public Sector Sector { get; private set; }

        private static Shader Shader;
        private static Shader sharedShader;
        private static Texture2D sharedTexture;

        private Texture2D blockTexture;
        private Texture2D atlasTexture;

        private SimpleBlock simple;

        public List<Chunk> Chunks { get; private set; } = new List<Chunk>();
        private List<Chunk> MeshesTogenerate = new List<Chunk>();
        private Dictionary<Vector3SByte, Chunk> chunkDictionary = new Dictionary<Vector3SByte, Chunk>();
        private Tag tag;
        private string _entityMassString = "0 tn";
        public BoundingBox GeometryBoundingBox { get; private set; }

        public SpaceEntity(int id, Vector3 positionWorld, Sector sector)
        {
            EntityID = id;
            Position = positionWorld;
            PositionWorld = positionWorld;
            Sector = sector;
            BoundingBox = new BoundingBox(positionWorld, new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));

            octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);
            Shader = ShaderManager.GetShader("Shaders/block");
            GeometryBoundingBox = new BoundingBox(positionWorld, Vector3.Zero);

            InitializeSharedResources();
            simple = new SimpleBlock(sharedShader, sharedTexture, PositionWorld);
            simple.Scale = new Vector3(1, 1, 1);
            simple.Position = BoundingBox.Center;

            blockTexture = GameBlocks.BlocksTexture;
            atlasTexture = GameBlocks.LightAtlas;
            // BoundingBox.CreateFromMinMax(GeometryMin, GeometryMax)
            tag = CreateTag(GeometryBoundingBox);

            RecalculateMass();
        }

        public void AddChunks(Chunk[] chunks, bool generateMesh)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];

                octree.Add(chunk, new BoundingBox(chunk.PositionWorld, Vector3.One * Chunk.Size));
                Chunks.Add(chunk);
                chunkDictionary.Add(chunk.PositionIndex, chunk);
                chunk.OnChunkModified += UpdateEntityGeometryMinMax;

                UpdateNeighbors(chunk);

            }

            for (int i = 0; i < chunks.Length; i++)
            {
                if (generateMesh)
                {
                    chunks[i].GenerateMesh();
                }
                else
                {
                    MeshesTogenerate.Add(chunks[i]);
                }


            }


            RecalculateGeometryBoundingBox();
        }

        public void GenerateMesh()
        {
            foreach (var chunk in MeshesTogenerate)
            {
                chunk.GenerateMesh();
            }
            MeshesTogenerate.Clear();
        }

        public void AddChunk(Chunk chunk)
        {
            octree.Add(chunk, new BoundingBox(chunk.PositionWorld, Vector3.One * Chunk.Size));
            Chunks.Add(chunk);
            chunkDictionary.Add(chunk.PositionIndex, chunk);
            chunk.OnChunkModified += UpdateEntityGeometryMinMax;
            chunk.GenerateMesh();
            UpdateNeighbors(chunk);
            RecalculateGeometryBoundingBox();
        }

        public void RemoveChunk(Chunk chunk)
        {
            octree.Remove(chunk);
            Chunks.Remove(chunk);
            chunkDictionary.Remove(chunk.PositionIndex);
            chunk.OnChunkModified -= UpdateEntityGeometryMinMax;
            chunk.Dispose();

            UpdateNeighbors(chunk, true);
            RecalculateGeometryBoundingBox();

            if (Chunks.Count == 0)
            {
                DeleteSpaceEntity();
            }
        }

        private void DeleteSpaceEntity()
        {

            Sector.RemoveEntity(this);
        }

        public static void InitializeSharedResources()
        {
            if (sharedShader == null)
            {
                sharedShader = ShaderManager.GetShader("Shaders/colored");
            }

            if (sharedTexture == null)
            {
                sharedTexture = TextureManager.GetTexture("Resources/Textures/selector.png", true);
            }
        }

        public void RecalculateMass(int chunkMassDifference)
        {
            var x = (long)Mass + chunkMassDifference;
            if (x >= 0)
            {
                Mass = (ulong)(x);
            }
            else
            {
                Mass = 0;
                Debug.Error("SpaceEntity mass was negative!");
            }

            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        public void RecalculateMass()
        {
            Mass = 0;
            for (int i = 0; i < Chunks.Count; i++)
            {
                Mass = (ulong)((long)Mass + Chunks[i].Mass);
            }

            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        private void UpdateEntityGeometryMinMax(Chunk chunk)
        {
            RecalculateGeometryBoundingBox();

            if (tag != null)
                tag.WorldPosition = GeometryBoundingBox.Center;
        }

        private Tag CreateTag(BoundingBox boundingBox)
        {
            var tag = new Tag("", boundingBox.Center, Color4.DarkGreen);
            tag.TextAlignment = Tag.Alignment.Right;
            TagManager.RegisterTag(tag);

            return tag;
        }

        private void RecalculateGeometryBoundingBox()
        {
            GeometryBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
            if (Chunks.Count == 0)
            {
                return;
            }

            Vector3 min = Chunks[0].GeometryBoundingBox.Min;
            Vector3 max = Chunks[0].GeometryBoundingBox.Max;

            foreach (var chunk in Chunks)
            {
                if (chunk.Mass > 0)
                {
                    min = Vector3.ComponentMin(min, chunk.GeometryBoundingBox.Min);
                    max = Vector3.ComponentMax(max, chunk.GeometryBoundingBox.Max);
                }
            }

            GeometryBoundingBox = BoundingBox.CreateFromMinMax(min, max);
        }

        public Vector3SByte GetChunkIndex(Vector3 worldPosition)
        {
            Vector3 relativePosition = worldPosition - PositionWorld;

            int indexX = (int)MathF.Floor(relativePosition.X / Chunk.Size);
            int indexY = (int)MathF.Floor(relativePosition.Y / Chunk.Size);
            int indexZ = (int)MathF.Floor(relativePosition.Z / Chunk.Size);

            return new Vector3SByte((sbyte)indexX, (sbyte)indexY, (sbyte)indexZ);
        }

        private static readonly Vector3SByte[] Directions = new Vector3SByte[]
        {
            new Vector3SByte(1, 0, 0), // X+
            new Vector3SByte(-1, 0, 0), // X-
            new Vector3SByte(0, 1, 0), // Y+
            new Vector3SByte(0, -1, 0), // Y-
            new Vector3SByte(0, 0, 1), // Z+
            new Vector3SByte(0, 0, -1) // Z-
        };

        private void UpdateNeighbors(Chunk chunk, bool removing = false)
        {
            foreach (var dir in Directions)
            {
                Vector3SByte neighborCoord = chunk.PositionIndex + dir;
                if (chunkDictionary.TryGetValue(neighborCoord, out Chunk neighbor))
                {
                    if (removing)
                    {
                        neighbor.RemoveNeighbor(chunk);
                        chunk.RemoveNeighbor(neighbor);
                    }
                    else
                    {
                        neighbor.AddNeighbor(chunk);
                        chunk.AddNeighbor(neighbor);
                    }
                }
            }
        }


        public void Update()
        {
            Camera camera = Camera.Main;

            if (camera != null)
            {
                tag.Text = $"{(int)Vector3.Distance(GeometryBoundingBox.Center, camera.Position)} m\n" +
                           $"{_entityMassString} tn";
            }
        }

        public bool Raycast(Ray ray, out VoxelPhysics.HitInfo hitInfo)
        {
            hitInfo = new VoxelPhysics.HitInfo();
            //List<Chunk> chunks = new List<Chunk>();
            // octree.GetColliding(chunks, ray, SizeBlocks);     working?

            VisualDebug.DrawPosition(PositionWorld, Color4.Red);
            foreach (var c in Chunks)
            {
                if (c.Raycast(ray, out hitInfo)) return true;
            }

            if (Chunks.Count == 0) return false;

            return false;
        }

        public void Render(Camera camera)
        {
            if (VisualDebug.ShowDebug)
            {
                VisualDebug.DrawPosition(PositionWorld, Color4.Cyan);
                VisualDebug.DrawPosition(GeometryBoundingBox.Center, Color4.Orange);
            }

            /*if (simple != null)
            {
                simple.Shader.SetVector4("color", new Vector4(0, 0, 1, 1));
                simple.Render(Camera.Main);
                VisualDebug.DrawPosition(Position, Color4.Cyan);
            }*/

            blockTexture.Use(TextureUnit.Texture0);
            atlasTexture.Use(TextureUnit.Texture1);
            for (int i = 0; i < Chunks.Count; i++)
            {
                Chunks[i].Render(Shader);
            }

            VisualDebug.DrawBoundingBox(GeometryBoundingBox, Color4.Orange);
        }

        public bool IsColliding(BoundingVolume volume)
        {
            bool c = false;

            for (int i = 0; i < Chunks.Count; i++)
            {
                c = Chunks[i].IsColliding(volume);

                if (c) return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (tag != null)
            {
                TagManager.UnregisterTag(tag);
            }

            simple?.Dispose();
        }
    }
}