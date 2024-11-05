// Chunk.cs
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game.Generation;
using Spacebox.Game.Lighting;
using Spacebox.Game.Rendering;
using Spacebox.GUI;
using Spacebox.Managers;

namespace Spacebox.Game
{
    public class Chunk : IDisposable
    {
        public const sbyte Size = 32;
        public Vector3 Position { get; private set; }
        public Block[,,] Blocks { get; private set; }
        public bool ShowChunkBounds { get; set; } = true;
        public bool MeasureGenerationTime { get; set; } = false;
        public bool IsModified { get; private set; } = false;
        public bool IsGenerated { get; private set; } = false;

        private Mesh _mesh;
        private readonly MeshGenerator _meshGenerator;
        private readonly LightManager _lightManager;
        private bool _isLoadedOrGenerated = false;

        private BlockDestructionManager destructionManager;

        private BoundingBox boundingBox;

        private Tag tag;
        public Chunk(Vector3 position)
            : this(position, null, isLoaded: false)
        {

            CreateBoundingBox();
        }

        // Внутренний конструктор с передачей BlockDestructionManager
        internal Chunk(Vector3 position, Block[,,] loadedBlocks, bool isLoaded)
        {
            Position = position;
            Blocks = new Block[Size, Size, Size];
            CreateBoundingBox();
            this.destructionManager = new BlockDestructionManager(Camera.Main);

            if (isLoaded && loadedBlocks != null)
            {
                Array.Copy(loadedBlocks, Blocks, loadedBlocks.Length);
                IsGenerated = true;
            }
            else
            {
                BlockGenerator blockGenerator = new BlockGenerator(Blocks, position);
                blockGenerator.GenerateSphereBlocks();
                IsGenerated = true;
            }

            _lightManager = new LightManager(Blocks);
            _lightManager.PropagateLight();

            _meshGenerator = new MeshGenerator(Blocks, MeasureGenerationTime);
            _mesh = _meshGenerator.GenerateMesh();
            _isLoadedOrGenerated = true;
        }

        private void CreateBoundingBox()
        {
            Vector3 chunkMin = Position;
            Vector3 chunkMax = Position + new Vector3(Size);
            boundingBox = BoundingBox.CreateFromMinMax(chunkMin, chunkMax);
            tag = new Tag("", boundingBox.Center, Color4.DarkGreen);
            //GameConsole.Debug(boundingBox.Center.ToString());
            TagManager.RegisterTag(tag);

        }

        public void GenerateMesh()
        {
            if (!_isLoadedOrGenerated) return;

            _lightManager.PropagateLight();
            Mesh newMesh = _meshGenerator.GenerateMesh();
            _mesh.Dispose();
            _mesh = newMesh;
        }

        public void Shift(Vector3 shift)
        {
            Position -= shift;
        }

        public void Draw(Shader shader)
        {
            if (!_isLoadedOrGenerated) return;

            if(Camera.Main.Frustum.IsInFrustum(boundingBox))
            {
                Matrix4 model = Matrix4.CreateTranslation(Position);
                shader.SetMatrix4("model", model);
                _mesh.Draw(shader);

                if (ShowChunkBounds && Spacebox.Common.VisualDebug.ShowDebug)
                {

                    VisualDebug.DrawBoundingBox(boundingBox, new Color4(0.5f, 0f, 0.5f, 1f));
                }

                destructionManager.Render();

            }

           

        }

        public void SetBlock(int x, int y, int z, Block block)
        {
            if (!IsInRange(x, y, z))
                return;

            Blocks[x, y, z] = block;
            IsModified = true;

            if (block.LightLevel > 0f)
            {
                Blocks[x, y, z].LightLevel = block.LightLevel;
                Blocks[x, y, z].LightColor = block.LightColor;
            }

            GenerateMesh();
        }

        public void RemoveBlock(int x, int y, int z)
        {
            if (!IsInRange(x, y, z))
                return;

            Vector3 worldBlockPosition = new Vector3(x, y, z);
            Console.WriteLine(Blocks[x, y, z].LightColor);
            destructionManager?.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor);

            Blocks[x, y, z] = GameBlocks.CreateFromId(0);

            

            IsModified = true;

            GenerateMesh();

        }

        public Block GetBlock(Vector3 pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
                return Blocks[x, y, z];
            return GameBlocks.CreateFromId(0);
        }

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }

        public bool Raycast(Ray ray, out Vector3 hitPosition, out Vector3i hitBlockPosition, out Vector3 hitNormal, float maxDistance = 100f)
        {
            hitPosition = Vector3.Zero;
            hitBlockPosition = new Vector3i(-1, -1, -1);
            hitNormal = Vector3.Zero;

            if (!_isLoadedOrGenerated) return false;

            Vector3 rayOrigin = ray.Origin - Position;
            Vector3 rayDirection = ray.Direction;

            int x = (int)MathF.Floor(rayOrigin.X);
            int y = (int)MathF.Floor(rayOrigin.Y);
            int z = (int)MathF.Floor(rayOrigin.Z);

            float deltaDistX = rayDirection.X == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.X);
            float deltaDistY = rayDirection.Y == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.Y);
            float deltaDistZ = rayDirection.Z == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.Z);

            int stepX = rayDirection.X < 0 ? -1 : 1;
            int stepY = rayDirection.Y < 0 ? -1 : 1;
            int stepZ = rayDirection.Z < 0 ? -1 : 1;

            float sideDistX = rayDirection.X == 0 ? float.MaxValue : (rayDirection.X < 0 ? (rayOrigin.X - x) : (x + 1.0f - rayOrigin.X)) * deltaDistX;
            float sideDistY = rayDirection.Y == 0 ? float.MaxValue : (rayDirection.Y < 0 ? (rayOrigin.Y - y) : (y + 1.0f - rayOrigin.Y)) * deltaDistY;
            float sideDistZ = rayDirection.Z == 0 ? float.MaxValue : (rayDirection.Z < 0 ? (rayOrigin.Z - z) : (z + 1.0f - rayOrigin.Z)) * deltaDistZ;

            float distanceTraveled = 0f;
            int side = -1;

            while (distanceTraveled < maxDistance)
            {
                if (IsInRange(x, y, z))
                {
                    Block block = Blocks[x, y, z];
                    if (!block.IsAir())
                    {
                        hitBlockPosition = new Vector3i(x, y, z);
                        hitPosition = ray.Origin + ray.Direction * distanceTraveled;

                        switch (side)
                        {
                            case 0:
                                hitNormal = new Vector3(-stepX, 0, 0);
                                break;
                            case 1:
                                hitNormal = new Vector3(0, -stepY, 0);
                                break;
                            case 2:
                                hitNormal = new Vector3(0, 0, -stepZ);
                                break;
                        }

                        return true;
                    }
                }
                else
                {
                    return false;
                }

                if (sideDistX < sideDistY)
                {
                    if (sideDistX < sideDistZ)
                    {
                        x += stepX;
                        distanceTraveled = sideDistX;
                        sideDistX += deltaDistX;
                        side = 0;
                    }
                    else
                    {
                        z += stepZ;
                        distanceTraveled = sideDistZ;
                        sideDistZ += deltaDistZ;
                        side = 2;
                    }
                }
                else
                {
                    if (sideDistY < sideDistZ)
                    {
                        y += stepY;
                        distanceTraveled = sideDistY;
                        sideDistY += deltaDistY;
                        side = 1;
                    }
                    else
                    {
                        z += stepZ;
                        distanceTraveled = sideDistZ;
                        sideDistZ += deltaDistZ;
                        side = 2;
                    }
                }
            }

            return false;
        }

        public void ChangeBlockColor(Block block, Vector3 color, bool regenerateMesh)
        {
            block.Color = color;

            if (regenerateMesh)
            {
                GenerateMesh();
            }
            
        }


        public void Test(Astronaut player)
        {
            if (!_isLoadedOrGenerated) return;

            if (Debug.IsVisible) return;


            if (Input.IsKeyDown(Keys.P))
            {
                ChunkSaveLoadManager.SaveChunk(this);
            }
           
            tag.Text =  (int)Vector3.Distance(boundingBox.Center, player.Position) + " m";
                destructionManager.Update();

            Vector3 rayOrigin = player.Position;

            Vector3 rayDirection = player.Front;
            float maxDistance = 5f;

            Ray ray = new Ray(rayOrigin, rayDirection, maxDistance);

            bool hit = Raycast(ray, out Vector3 hitPosition, out Vector3i hitBlockPosition, out Vector3 hitNormal);

            if (hit)
            {

                Vector3 worldBlockPosition = hitBlockPosition + Position;
                VisualDebug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f), Vector3.One * 1.01f), Color4.White);

                Block selectedBlock = Blocks[hitBlockPosition.X, hitBlockPosition.Y, hitBlockPosition.Z];

                if(Input.IsKeyDown(Keys.R))
                {
                    
                    ChangeBlockColor(selectedBlock, new Vector3(1, 0, 0), true);
                    GenerateMesh();
                }
                if (Input.IsKeyDown(Keys.T))
                {
                    Blocks[hitBlockPosition.X, hitBlockPosition.Y, hitBlockPosition.Z].Color = new Vector3(1, 1, 0);

                    GenerateMesh();
                }
                if (Input.IsKeyDown(Keys.Z))
                {
                    Blocks[hitBlockPosition.X, hitBlockPosition.Y, hitBlockPosition.Z].Color = new Vector3(1, 1, 1);

                    GenerateMesh();
                }



                VisualDebug.DrawBoundingSphere(new BoundingSphere(hitPosition, 0.05f), Color4.Red);

                if (selectedBlock.BlockId == 20 || selectedBlock.BlockId == 22)
                {
                    float dis = Vector3.Distance(player.Position, hitBlockPosition);

                    if (dis < 3f)
                    CenteredText.Show();
                    else
                        CenteredText.Hide();
                }
                else
                {
                    CenteredText.Hide();
                }

                if (Input.IsMouseButtonDown(MouseButton.Left))
                {
                    RemoveBlock(hitBlockPosition.X, hitBlockPosition.Y, hitBlockPosition.Z);
                }

                if (Input.IsMouseButtonDown(MouseButton.Middle))
                {
                    Block b = GetBlock(hitBlockPosition);
                    Console.WriteLine($"{b} Pos: {hitBlockPosition}");
                }

                if (Input.IsMouseButtonDown(MouseButton.Right))
                {
                    Vector3i placeBlockPosition = hitBlockPosition + new Vector3i((int)hitNormal.X, (int)hitNormal.Y, (int)hitNormal.Z);

                    if (IsInRange(placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z) &&
                        Blocks[placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z].IsAir())
                    {
                        Block newBlock = GameBlocks.CreateFromId(player.CurrentBlockId);
                        SetBlock(placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z, newBlock);
                    }
                }
            }
            else
            {
                float placeDistance = 5f;
                Vector3 placePosition = rayOrigin + rayDirection * placeDistance;
                Vector3 localPosition = placePosition - Position;

                int x = (int)MathF.Floor(localPosition.X);
                int y = (int)MathF.Floor(localPosition.Y);
                int z = (int)MathF.Floor(localPosition.Z);

                Vector3 worldBlockPosition = new Vector3(x, y, z) + Position;
                Spacebox.Common.VisualDebug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f), Vector3.One * 1.01f), Color4.Gray);

                if(CenteredText.IsVisible)
                {
                    CenteredText.Hide();
                }

                if (Input.IsMouseButtonDown(MouseButton.Right) &&
                    IsInRange(x, y, z) &&
                    Blocks[x, y, z].IsAir())
                {
                    Block newBlock = GameBlocks.CreateFromId(player.CurrentBlockId);
                    SetBlock(x, y, z, newBlock);
                }
            }
        }

        public void Dispose()
        {
            _mesh?.Dispose();
        }
    }
}
