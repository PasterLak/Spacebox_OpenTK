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

        private List<Chunk> chunks = new List<Chunk>();
        private Dictionary<Vector3SByte, Chunk> chunkDictionary = new Dictionary<Vector3SByte, Chunk>();
        private Tag tag;

        private BoundingBox geometryBoundingBox;

        public SpaceEntity(Vector3 positionWorld, Sector sector, bool oneChunk)
        {
            Debug.Success("<<<< New entity! " + positionWorld + "local 0 10 0: " 
                          + LocalToWorld(new Vector3(0, 10, 0), this));
            PositionWorld = positionWorld;
            Sector = sector;
            BoundingBox = new BoundingBox(positionWorld, new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));

            octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);
            Shader = ShaderManager.GetShader("Shaders/block");
            geometryBoundingBox = new BoundingBox(positionWorld, Vector3.Zero);
   
            AddChunk(new Chunk(positionWorld));

            if (!oneChunk)
            {
                AddChunk(new Chunk(positionWorld + new Vector3(0, Chunk.Size, 0)));
            }

            //GeometryMin = c.GeometryMin;
            //GeometryMax = c.GeometryMax;
            InitializeSharedResources();
            simple = new SimpleBlock(sharedShader, sharedTexture, PositionWorld);
            simple.Scale = new Vector3(1, 1, 1);
            simple.Position = BoundingBox.Center;

            blockTexture = GameBlocks.BlocksTexture;
            atlasTexture = GameBlocks.LightAtlas;
// BoundingBox.CreateFromMinMax(GeometryMin, GeometryMax)
            tag = CreateTag(geometryBoundingBox);
        }
        
        public void AddChunk(Chunk chunk)
        {
            octree.Add(chunk, new BoundingBox(chunk.Position, Vector3.One * Chunk.Size));
            chunks.Add(chunk);
           // chunkDictionary.Add(chunk.Index, chunk);
            chunk.OnChunkModified += UpdateEntityGeometryMinMax;
            chunk.GenerateMesh();

            RecalculateGeometryBoundingBox();
        }

        public void RemoveChunk(Chunk chunk)
        {
            octree.Remove(chunk);
            chunks.Remove(chunk);
            //chunkDictionary.Remove(chunk.Index);
            chunk.OnChunkModified -= UpdateEntityGeometryMinMax;
            chunk.Dispose();

            RecalculateGeometryBoundingBox();
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

        private void UpdateEntityGeometryMinMax(Chunk chunk)
        {
            RecalculateGeometryBoundingBox();

            if (tag != null)
                tag.WorldPosition = geometryBoundingBox.Center;
        }

        private Tag CreateTag(BoundingBox boundingBox)
        {
            var tag = new Tag("", boundingBox.Center + new Vector3(0.5f, 0.5f, 0.5f), Color4.DarkGreen);

            TagManager.RegisterTag(tag);

            return tag;
        }

        private void RecalculateGeometryBoundingBox()
        {
            if (chunks.Count == 0)
            {
                geometryBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
                return;
            }

            Vector3 min = chunks[0].GeometryBoundingBox.Min;
            Vector3 max = chunks[0].GeometryBoundingBox.Max;

            foreach (var chunk in chunks)
            {
                min = Vector3.ComponentMin(min, chunk.GeometryBoundingBox.Min);
                max = Vector3.ComponentMax(max, chunk.GeometryBoundingBox.Max);
            }

            geometryBoundingBox = BoundingBox.CreateFromMinMax(min, max);
        }


        public void Update()
        {
            Camera camera = Camera.Main;

            if (camera != null)
                tag.Text = (int)Vector3.Distance(BoundingBox.Center + new Vector3(0.5f, 0.5f, 0.5f), camera.Position) +
                           " m";

            VisualDebug.DrawBoundingBox(geometryBoundingBox, Color4.Orange);
        }

        public bool Raycast(Ray ray, out VoxelPhysics.HitInfo hitInfo)
        {
            hitInfo = new VoxelPhysics.HitInfo();
            //List<Chunk> chunks = new List<Chunk>();
            // octree.GetColliding(chunks, ray, SizeBlocks);     working?

            foreach (var c in chunks)
            {
                if (c.Raycast(ray, out hitInfo)) return true;
            }

            if (chunks.Count == 0) return false;

            return false;
        }

        public void Render(Camera camera)
        {
            VisualDebug.DrawPosition(PositionWorld, Color4.Cyan);

            /*if (simple != null)
            {
                simple.Shader.SetVector4("color", new Vector4(0, 0, 1, 1));
                simple.Render(Camera.Main);
                VisualDebug.DrawPosition(Position, Color4.Cyan);
            }*/

            blockTexture.Use(TextureUnit.Texture0);
            atlasTexture.Use(TextureUnit.Texture1);
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Render(Shader);
            }
        }

        public bool IsColliding(BoundingVolume volume)
        {
            bool c = false;

            for (int i = 0; i < chunks.Count; i++)
            {
                c = chunks[i].IsColliding(volume);

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