using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;

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
        public Vector3 Position { get; private set; }

        public Sector Sector { get; private set; }

        private static Shader sharedShader;
        private static Texture2D sharedTexture;

        private SimpleBlock simple;

        public SpaceEntity(Vector3 position, Sector sector)
        {
            Position = position;
            Sector = sector;
            BoundingBox = new BoundingBox(position + new Vector3(SizeBlocksHalf, SizeBlocksHalf, SizeBlocksHalf), new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));

            octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);
           

            Chunk c = new Chunk(position);
            octree.Add(c, new BoundingBox(position, Vector3.One * Chunk.Size));

            //simple = new SimpleBlock(sharedShader, sharedTexture, Position);
            //simple.Scale = new Vector3(1, 1, 1);
            //simple.Position = Position;
        }

        public static void InitializeSharedResources()
        {


            if (sharedShader == null)
            {
                sharedShader = ShaderManager.GetShader("Shaders/textured");

            }

            if (sharedTexture == null)
            {
                sharedTexture = TextureManager.GetTexture("Resources/Textures/selector.png", true);
            }

            
        }

        public void Render(Camera camera)
        {
            if(simple != null)
            {
                simple.Shader.SetVector4("color", new Vector4(0, 0, 1, 1));
                simple.Render(Camera.Main);
                VisualDebug.DrawPosition(Position, Color4.Cyan);
            }
        }

        public void Dispose()
        {
            simple?.Dispose();
        }
    }
}
