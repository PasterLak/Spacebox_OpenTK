using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Game.Generation;
using Spacebox.Game.Lighting;
using Spacebox.Game.Rendering;
using System;

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

        /// <summary>
        /// Constructor for generating new chunks.
        /// </summary>
        /// <param name="position">The position of the chunk.</param>
        public Chunk(Vector3 position) : this(position, null, isLoaded: false)
        {
        }

        /// <summary>
        /// Private constructor used by ChunkSaveLoadManager for loading chunks.
        /// </summary>
        /// <param name="position">The position of the chunk.</param>
        /// <param name="loadedBlocks">The loaded Blocks array.</param>
        /// <param name="isLoaded">Indicates whether the chunk is loaded from saved data.</param>
        internal Chunk(Vector3 position, Block[,,] loadedBlocks, bool isLoaded)
        {
            Position = position;
            Blocks = new Block[Size, Size, Size];

            if (isLoaded && loadedBlocks != null)
            {
                Array.Copy(loadedBlocks, Blocks, loadedBlocks.Length);
                IsGenerated = true;
            }
            else
            {
                // Initialize BlockGenerator and generate blocks
                BlockGenerator blockGenerator = new BlockGenerator(Blocks, position);
                blockGenerator.GenerateSphereBlocks();
                IsGenerated = true;
            }

            // Initialize LightManager and propagate light
            _lightManager = new LightManager(Blocks);
            _lightManager.PropagateLight();

            // Initialize MeshGenerator and generate initial mesh
            _meshGenerator = new MeshGenerator(Blocks, MeasureGenerationTime);
            _mesh = _meshGenerator.GenerateMesh();
            _isLoadedOrGenerated = true;
        }

        /// <summary>
        /// Generates or updates the mesh for the chunk.
        /// </summary>
        public void GenerateMesh()
        {
            if (!_isLoadedOrGenerated) return;

            _lightManager.PropagateLight();
            Mesh newMesh = _meshGenerator.GenerateMesh();
            _mesh.Dispose();
            _mesh = newMesh;
        }

        /// <summary>
        /// Shifts the chunk's position by the specified vector.
        /// </summary>
        /// <param name="shift">The vector to shift the chunk by.</param>
        public void Shift(Vector3 shift)
        {
            Position -= shift;
            // Additional logic if necessary
        }

        /// <summary>
        /// Draws the chunk using the provided shader.
        /// </summary>
        /// <param name="shader">The shader to use for drawing.</param>
        public void Draw(Shader shader)
        {
            if (!_isLoadedOrGenerated) return;

            Matrix4 model = Matrix4.CreateTranslation(Position);
            shader.SetMatrix4("model", model);
            _mesh.Draw(shader);

            if (ShowChunkBounds && Spacebox.Common.Debug.ShowDebug)
            {
                Vector3 chunkMin = Position;
                Vector3 chunkMax = Position + new Vector3(Size);
                BoundingBox chunkBounds = BoundingBox.CreateFromMinMax(chunkMin, chunkMax);
                Spacebox.Common.Debug.DrawBoundingBox(chunkBounds, new Color4(0.5f, 0f, 0.5f, 1f));
            }
        }

        /// <summary>
        /// Sets a block at the specified coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate within the chunk.</param>
        /// <param name="y">The y-coordinate within the chunk.</param>
        /// <param name="z">The z-coordinate within the chunk.</param>
        /// <param name="block">The block to set.</param>
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

        /// <summary>
        /// Removes a block at the specified coordinates, setting it to Air.
        /// </summary>
        /// <param name="x">The x-coordinate within the chunk.</param>
        /// <param name="y">The y-coordinate within the chunk.</param>
        /// <param name="z">The z-coordinate within the chunk.</param>
        public void RemoveBlock(int x, int y, int z)
        {
            if (!IsInRange(x, y, z))
                return;

            Blocks[x, y, z] = GameBlocks.CreateFromId(0); // Air
            IsModified = true;

            GenerateMesh();
        }

        /// <summary>
        /// Retrieves a block based on a position vector.
        /// </summary>
        /// <param name="pos">The position vector within the chunk.</param>
        /// <returns>The block at the specified position.</returns>
        public Block GetBlock(Vector3 pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        /// <summary>
        /// Retrieves a block based on x, y, z coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate within the chunk.</param>
        /// <param name="y">The y-coordinate within the chunk.</param>
        /// <param name="z">The z-coordinate within the chunk.</param>
        /// <returns>The block at the specified coordinates.</returns>
        public Block GetBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
                return Blocks[x, y, z];
            return GameBlocks.CreateFromId(0); // Air
        }

        /// <summary>
        /// Checks if the specified coordinates are within the chunk's bounds.
        /// </summary>
        /// <param name="x">The x-coordinate to check.</param>
        /// <param name="y">The y-coordinate to check.</param>
        /// <param name="z">The z-coordinate to check.</param>
        /// <returns>True if within range; otherwise, false.</returns>
        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }

        /// <summary>
        /// Performs a raycast within the chunk to detect block intersections.
        /// </summary>
        /// <param name="ray">The ray to cast.</param>
        /// <param name="hitPosition">The position where the ray hit a block.</param>
        /// <param name="hitBlockPosition">The block coordinates that were hit.</param>
        /// <param name="hitNormal">The normal of the hit block face.</param>
        /// <param name="maxDistance">The maximum distance to cast the ray.</param>
        /// <returns>True if a block was hit; otherwise, false.</returns>
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

        /// <summary>
        /// Handles player interactions with the chunk, including saving and modifying blocks.
        /// </summary>
        /// <param name="player">The player interacting with the chunk.</param>
        public void Test(Astronaut player)
        {
            if (!_isLoadedOrGenerated) return;

            // Handle saving on pressing P
            if (Input.IsKeyDown(Keys.P))
            {
                ChunkSaveLoadManager.SaveChunk(this);
            }

            // Existing test logic
            Vector3 rayOrigin = player.Position;
            Vector3 rayDirection = player.Front;
            float maxDistance = 100f;
            Ray ray = new Ray(rayOrigin, rayDirection, maxDistance);

            bool hit = Raycast(ray, out Vector3 hitPosition, out Vector3i hitBlockPosition, out Vector3 hitNormal);

            if (hit)
            {
                Vector3 worldBlockPosition = hitBlockPosition + Position;
                Spacebox.Common.Debug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f) + hitNormal,
                    Vector3.One * 1.01f), Color4.White);

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
                Spacebox.Common.Debug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f),
                    Vector3.One * 1.01f), Color4.Gray);

                if (Input.IsMouseButtonDown(MouseButton.Right) &&
                    IsInRange(x, y, z) &&
                    Blocks[x, y, z].IsAir())
                {
                    Block newBlock = GameBlocks.CreateFromId(player.CurrentBlockId);


                    SetBlock(x, y, z, newBlock);
                }
            }
        }

        /// <summary>
        /// Disposes of the mesh resources.
        /// </summary>
        public void Dispose()
        {
            _mesh?.Dispose();
        }
    }
}
