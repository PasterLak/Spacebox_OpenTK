﻿using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;
using OpenTK.Graphics.OpenGL4;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Effects;

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

        private Vector3 SumPosCenterOfMass;
        public Vector3 CenterOfMass { get; private set; }
        public bool IsModified { get; private set; } = false;
        public void SetModified() { if (!IsModified) IsModified = true; }
        public Sector Sector { get; private set; }

        private static Shader Shader;
        private static Shader sharedShader;
        private static Texture2D sharedTexture;

        private Texture2D blockTexture;
        private Texture2D atlasTexture;

        public List<Chunk> Chunks { get; private set; } = new List<Chunk>();
        private List<Chunk> MeshesTogenerate = new List<Chunk>();
        public Dictionary<Vector3SByte, Chunk> ChunkDictionary { get; private set; } = new Dictionary<Vector3SByte, Chunk>();
        private Tag tag;
        private string _entityMassString = "0 tn";
        public BoundingBox GeometryBoundingBox { get; private set; }

        public Particle StarParticle;
        public StarsEffect StarsEffect { get; private set; }
        public SpaceEntity(int id, Vector3 positionWorld, Sector sector)
        {
            EntityID = id;
            Position = positionWorld;
            PositionWorld = positionWorld;
            Sector = sector;
            BoundingBox = new BoundingBox(positionWorld, new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
            SumPosCenterOfMass = positionWorld;
            octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);
            Shader = ShaderManager.GetShader("Shaders/block");
            GeometryBoundingBox = new BoundingBox(positionWorld, Vector3.Zero);

            InitializeSharedResources();

            blockTexture = GameBlocks.BlocksTexture;
            atlasTexture = GameBlocks.LightAtlas;
            // BoundingBox.CreateFromMinMax(GeometryMin, GeometryMax)
            tag = CreateTag(GeometryBoundingBox.Center);
            CreateStar();
        }

        public SpaceEntity(Vector3 positionWorld, Sector sector)
        {
            EntityID = 0;
            Position = positionWorld;
            PositionWorld = positionWorld;
            Sector = sector;
            BoundingBox = new BoundingBox(positionWorld, new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
            SumPosCenterOfMass = positionWorld;
            octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);
            Shader = ShaderManager.GetShader("Shaders/block");
            GeometryBoundingBox = new BoundingBox(positionWorld, Vector3.Zero);

            InitializeSharedResources();

            blockTexture = GameBlocks.BlocksTexture;
            atlasTexture = GameBlocks.LightAtlas;
            // BoundingBox.CreateFromMinMax(GeometryMin, GeometryMax)
            tag = CreateTag(GeometryBoundingBox.Center);
            CreateStar();
        }
        private void CreateStar()
        {

            StarsEffect = new StarsEffect(World.Instance.Player);
            StarParticle = new Particle(GeometryBoundingBox.Center, Vector3.Zero, 9999999, new Vector4(1, 1, 1, 1), new Vector4(0, 0, 0, 0), 64);
            StarsEffect.ParticleSystem.AddParticle(StarParticle);
        }

        public void CreateFirstBlock(Block block)
        {
            if (Chunks.Count != 0)
            {
                Debug.Error("[spaceEntity] CreateFirstBlock was not possible: A chunk already exists");
                return;
            }

            Chunk chunk = new Chunk(new Vector3SByte(0, 0, 0), this, true);

            chunk.PlaceBlock(new Vector3Byte(0, 0, 0), block);
            AddChunk(chunk, false);
            chunk.GenerateMesh();

        }

        public void AddChunks(Chunk[] chunks, bool generateMesh)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];

                octree.Add(chunk, new BoundingBox(chunk.PositionWorld, Vector3.One * Chunk.Size));
                Chunks.Add(chunk);
                ChunkDictionary.Add(chunk.PositionIndex, chunk);
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
            RecalculateMass();
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
            AddChunk(chunk, true);
        }


        public void AddChunk(Chunk chunk, bool generateMesh = true)
        {
            octree.Add(chunk, new BoundingBox(chunk.PositionWorld, Vector3.One * Chunk.Size));
            Chunks.Add(chunk);
            ChunkDictionary.Add(chunk.PositionIndex, chunk);
            chunk.OnChunkModified += UpdateEntityGeometryMinMax;
            if (generateMesh)
                chunk.GenerateMesh();
            UpdateNeighbors(chunk);
            RecalculateGeometryBoundingBox();
            RecalculateMass();
        }

        public void RemoveChunk(Chunk chunk)
        {
            octree.Remove(chunk);
            Chunks.Remove(chunk);
            ChunkDictionary.Remove(chunk.PositionIndex);
            chunk.OnChunkModified -= UpdateEntityGeometryMinMax;
            chunk.Dispose();

            UpdateNeighbors(chunk, true);
            RecalculateGeometryBoundingBox();
            RecalculateMass();
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

            SumPosCenterOfMass = Vector3.Zero;

            for (int i = 0; i < Chunks.Count; i++) // opt
            {
                var chunkMass = Chunks[i].Mass;

                SumPosCenterOfMass += Chunks[i].GetCenterOfMass() * chunkMass;
            }

            CalculateCenterOfMass();
            CalculateGravityRadius();
            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        public void RecalculateMass()
        {
            Mass = 0;
            SumPosCenterOfMass = Vector3.Zero;

            for (int i = 0; i < Chunks.Count; i++)
            {
                var chunkMass = Chunks[i].Mass;
                Mass = (ulong)((long)Mass + chunkMass);

                SumPosCenterOfMass += Chunks[i].GetCenterOfMass() * chunkMass;
            }
            CalculateCenterOfMass();
            CalculateGravityRadius();
            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        private void UpdateEntityGeometryMinMax(Chunk chunk)
        {
            RecalculateGeometryBoundingBox();
            CalculateGravityRadius();
            // if (tag != null)
            //    tag.WorldPosition = GeometryBoundingBox.Center;
        }

        private Tag CreateTag(Vector3 worldPos)
        {
            var tag = new Tag("", worldPos, Color4.DarkGreen);
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

        public Vector3Byte WorldPositionToBlockInChunk(Vector3 worldPosition)
        {
            Vector3 relativePosition = worldPosition - PositionWorld;

            int localX = (int)MathF.Floor(relativePosition.X) % Chunk.Size;
            int localY = (int)MathF.Floor(relativePosition.Y) % Chunk.Size;
            int localZ = (int)MathF.Floor(relativePosition.Z) % Chunk.Size;

            if (localX < 0) localX += Chunk.Size;
            if (localY < 0) localY += Chunk.Size;
            if (localZ < 0) localZ += Chunk.Size;

            return new Vector3Byte((byte)localX, (byte)localY, (byte)localZ);
        }



        public bool TryPlaceBlock(Vector3 worldPosition, Block block)
        {

            if (!IsPositionWithinEntitySize(worldPosition)) return false;
            if (!IsPositionWithinGravityRadius(worldPosition)) return false;

            var chunkIndex = GetChunkIndex(worldPosition);

            var blockPos = WorldPositionToBlockInChunk(worldPosition);



            if (ChunkDictionary.TryGetValue(chunkIndex, out var chunk))
            {

                chunk.PlaceBlock(blockPos, block);
                return true;
            }
            else
            {
                Chunk newChunk = new Chunk(chunkIndex, this, true);
                AddChunk(newChunk, false);
                newChunk.PlaceBlock(blockPos, block);
                return true;
            }

            return false;
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
                if (ChunkDictionary.TryGetValue(neighborCoord, out Chunk neighbor))
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

        public bool IsPositionInChunk(Vector3 world, out Chunk chunk)
        {
            var index = GetChunkIndex(world);

            if (ChunkDictionary.TryGetValue(index, out chunk))
            {
                return true;
            }
            else
            {
                chunk = null;
                return false;
            }
        }

        public Vector3 WorldPositionToLocal(Vector3 world)
        {
            return world - PositionWorld;
        }
        public Vector3 LocalPositionToWorld(Vector3 local)
        {
            return local + PositionWorld;
        }
        public void Update()
        {
            Camera camera = Camera.Main;

            if (camera != null)
            {
                var dis = (int)Vector3.Distance(CenterOfMass, camera.Position);

                tag.SetFontSizeFromDistance(dis);

                if (VisualDebug.ShowDebug)
                {

                    bool isAsteroid = this as Asteroid != null;
                    tag.Text = $" {EntityID} {Name} isAsteroid: {isAsteroid}\n" +
                        $"Wpos: {Block.RoundVector3(PositionWorld)}\n" +
                        $"{dis} m\n" +
                           $"{_entityMassString} tn, gR: {(int)GravityRadius} m";
                }
                else
                {
                    tag.Text = $"{dis} m\n" +
                           $"{_entityMassString} tn";
                }

                //StarsEffect.Update();

            }
        }


        public Octree<Chunk> Octree => octree;

        public bool Raycast(Ray ray, out HitInfo hitInfo)
        {
            hitInfo = new HitInfo();

            if (Chunks.Count == 0) return false;

            if (VoxelPhysics.RaycastChunks(this, ray, out var crossedChunks))
            {
                //Debug.Log("Chunks found: " + crossedChunks.Count );

                foreach (var chunkHit in crossedChunks)
                {

                    var length = ray.Length - chunkHit.Distance;

                    if(length <= 0) continue;

                    Ray chunkRay = new Ray(chunkHit.HitPosition, ray.Direction, length);
                    // Debug.Log($"chunk check {chunkHit.Chunk.PositionIndex} origin {chunkRay.Origin}") ;


                    if (chunkHit.Chunk.Raycast(chunkRay, out hitInfo))
                    {
                        //  Debug.Log($"chunk {chunkHit.Chunk.PositionIndex} hit!");
                        return true;
                    }
                    else
                    {
                        // Debug.Log($"chunk {chunkHit.Chunk.PositionIndex} no hit");
                    }
                }
            }
            else
            {

            }

            return false;
        }


        public void RenderEffect(float disSqr)
        {

            var disMin = 300f * 300f;
            var disMax = 3000f * 3000f;

            float alpha;
            if (disSqr <= disMin)
            {
                alpha = 0f;
            }
            else if (disSqr >= disMax)
            {
                alpha = 1f;
            }
            else
            {
                alpha = (disSqr - disMin) / (disMax - disMin);
            }

            float size;

            const float sizeMin = Chunk.Size * 2;
            const float sizeMax = Chunk.Size * 2;

            if (disSqr <= disMin)
            {
                size = sizeMin;
            }
            else if (disSqr >= disMax)
            {
                size = sizeMax;
            }
            else
            {
                float t = (disSqr - disMin) / (disMax - disMin);
                size = sizeMin + (sizeMax - sizeMin) * t;
            }

            StarParticle.StartColor = new Vector4(1, 1, 1, alpha);
            StarParticle.EndColor = new Vector4(1, 1, 1, alpha);
            StarParticle.Size = size;

            StarsEffect.Update();
            StarsEffect.Render();
        }


        public void Render(Camera camera)
        {

            blockTexture.Use(TextureUnit.Texture0);
            atlasTexture.Use(TextureUnit.Texture1);
            for (int i = 0; i < Chunks.Count; i++)
            {
                if (Chunks[i].NeedsToRegenerateMesh)
                {
                    Chunks[i].GenerateMesh(false);
                    Chunks[i].NeedsToRegenerateMesh = false;
                }
                // VisualDebug.DrawPosition(Chunks[i].GetCenterOfMass(), 4, Color4.Green);
                Chunks[i].Render(Shader);

            }

            if (VisualDebug.ShowDebug)
            {
                VisualDebug.DrawBoundingBox(BoundingBox, Color4.Cyan);
                VisualDebug.DrawPosition(GeometryBoundingBox.Center, 6, Color4.Orange);

                VisualDebug.DrawBoundingBox(GeometryBoundingBox, Color4.Orange);
                VisualDebug.DrawPosition(CenterOfMass, 8, Color4.Lime);

                //VisualDebug.DrawSphere(CenterOfMass, GravityRadius, 16, Color4.Blue);
            }

        }

        public bool IsColliding(BoundingVolume volume, out CollideInfo collideInfo)
        {
            bool c = false;

            var collideInfo2 = new CollideInfo();

            for (int i = 0; i < Chunks.Count; i++)
            {
                c = Chunks[i].IsColliding(volume, out collideInfo);

                if (c) return true;
            }
            collideInfo = collideInfo2;
            return false;
        }

        public bool IsPositionWithinEntitySize(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - PositionWorld;
            return local.X >= -SizeBlocksHalf && local.X < SizeBlocksHalf
                && local.Y >= -SizeBlocksHalf && local.Y < SizeBlocksHalf
                && local.Z >= -SizeBlocksHalf && local.Z < SizeBlocksHalf;
        }

        public bool IsPositionWithinGeometryBounds(Vector3 worldPosition)
        {
            return GeometryBoundingBox.Contains(worldPosition);
        }

        public float GravityRadius { get; private set; }

        private const float GravityScale = 1.0f;

        private void CalculateGravityRadius()
        {
            if (Mass == 0)
            {
                GravityRadius = 0f;
                return;
            }

            GravityRadius = GravityScale * MathF.Pow((float)Mass, 0.32f);

            GravityRadius += GeometryBoundingBox.Diagonal;

            int dis = (int)Vector3.Distance(GeometryBoundingBox.Center, CenterOfMass);
            GravityRadius += dis;

            if (GravityRadius < 10)
            {
                GravityRadius = 10;
            }
        }
        public bool IsPositionWithinGravityRadius(Vector3 worldPosition)
        {
            float distanceSquared = Vector3.DistanceSquared(CenterOfMass, worldPosition);
            return distanceSquared <= GravityRadius * GravityRadius;
        }

        private void CalculateCenterOfMass()
        {
            if (Mass == 0)
            {
                CenterOfMass = GeometryBoundingBox.Center;
                tag.WorldPosition = CenterOfMass;
                return;
            }

            CenterOfMass = SumPosCenterOfMass / Mass;
            tag.WorldPosition = CenterOfMass;

            StarParticle.Position = CenterOfMass;
        }

        public static List<Chunk> RemoveBlocksInLocalBox(SpaceEntity entity, BoundingBox localBox)
        {
            var min = localBox.Min;
            var max = localBox.Max;

            int minX = (int)MathF.Floor(min.X);
            int minY = (int)MathF.Floor(min.Y);
            int minZ = (int)MathF.Floor(min.Z);
            int maxX = (int)MathF.Floor(max.X);
            int maxY = (int)MathF.Floor(max.Y);
            int maxZ = (int)MathF.Floor(max.Z);

            int chunkMinX = minX / Chunk.Size;
            int chunkMaxX = maxX / Chunk.Size;
            int chunkMinY = minY / Chunk.Size;
            int chunkMaxY = maxY / Chunk.Size;
            int chunkMinZ = minZ / Chunk.Size;
            int chunkMaxZ = maxZ / Chunk.Size;

            HashSet<Chunk> modified = new HashSet<Chunk>();

            for (int cx = chunkMinX; cx <= chunkMaxX; cx++)
            {
                for (int cy = chunkMinY; cy <= chunkMaxY; cy++)
                {
                    for (int cz = chunkMinZ; cz <= chunkMaxZ; cz++)
                    {
                        Vector3SByte idx = new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz);
                        if (!entity.ChunkDictionary.ContainsKey(idx)) continue;
                        Chunk c = entity.ChunkDictionary[idx];
                        if (c == null) continue;

                        int startX = Math.Max(0, minX - cx * Chunk.Size);
                        int endX = Math.Min(Chunk.Size - 1, maxX - cx * Chunk.Size);
                        int startY = Math.Max(0, minY - cy * Chunk.Size);
                        int endY = Math.Min(Chunk.Size - 1, maxY - cy * Chunk.Size);
                        int startZ = Math.Max(0, minZ - cz * Chunk.Size);
                        int endZ = Math.Min(Chunk.Size - 1, maxZ - cz * Chunk.Size);

                        for (int x = startX; x <= endX; x++)
                        {
                            for (int y = startY; y <= endY; y++)
                            {
                                for (int z = startZ; z <= endZ; z++)
                                {
                                    Block b = c.Blocks[x, y, z];
                                    if (b != null && !b.IsAir())
                                    {
                                        c.Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);
                                        c.IsModified = true;
                                        modified.Add(c);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return modified.ToList();
        }


        private static Chunk GetOrCreateChunk(SpaceEntity entity, Vector3SByte idx)
        {
            if (!entity.ChunkDictionary.TryGetValue(idx, out Chunk chunk) || chunk == null)
            {
                chunk = new Chunk(idx, entity, true);
                entity.ChunkDictionary[idx] = chunk;

                entity.octree.Add(chunk, new BoundingBox(chunk.PositionWorld, Vector3.One * Chunk.Size));
                entity.Chunks.Add(chunk);

                chunk.OnChunkModified += entity.UpdateEntityGeometryMinMax;

                entity.UpdateNeighbors(chunk);
                entity.RecalculateGeometryBoundingBox();
                entity.RecalculateMass();
            }
            return chunk;
        }

        public static List<Chunk> FillBlocksInLocalBox(SpaceEntity entity, BoundingBox localBox, short blockId)
        {
            var min = localBox.Min;
            var max = localBox.Max;

            int minX = (int)MathF.Floor(min.X);
            int minY = (int)MathF.Floor(min.Y);
            int minZ = (int)MathF.Floor(min.Z);
            int maxX = (int)MathF.Floor(max.X);
            int maxY = (int)MathF.Floor(max.Y);
            int maxZ = (int)MathF.Floor(max.Z);

            int chunkMinX = minX / Chunk.Size;
            int chunkMaxX = maxX / Chunk.Size;
            int chunkMinY = minY / Chunk.Size;
            int chunkMaxY = maxY / Chunk.Size;
            int chunkMinZ = minZ / Chunk.Size;
            int chunkMaxZ = maxZ / Chunk.Size;

            HashSet<Chunk> modified = new HashSet<Chunk>();

            for (int cx = chunkMinX; cx <= chunkMaxX; cx++)
            {
                for (int cy = chunkMinY; cy <= chunkMaxY; cy++)
                {
                    for (int cz = chunkMinZ; cz <= chunkMaxZ; cz++)
                    {
                        Vector3SByte idx = new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz);
                        Chunk c = GetOrCreateChunk(entity, idx);

                        int startX = Math.Max(0, minX - cx * Chunk.Size);
                        int endX = Math.Min(Chunk.Size - 1, maxX - cx * Chunk.Size);
                        int startY = Math.Max(0, minY - cy * Chunk.Size);
                        int endY = Math.Min(Chunk.Size - 1, maxY - cy * Chunk.Size);
                        int startZ = Math.Max(0, minZ - cz * Chunk.Size);
                        int endZ = Math.Min(Chunk.Size - 1, maxZ - cz * Chunk.Size);

                        for (int x = startX; x <= endX; x++)
                        {
                            for (int y = startY; y <= endY; y++)
                            {
                                for (int z = startZ; z <= endZ; z++)
                                {
                                    Block b = c.Blocks[x, y, z];
                                    if (b == null || b.IsAir())
                                    {
                                        c.Blocks[x, y, z] = GameBlocks.CreateBlockFromId(blockId);
                                        c.IsModified = true;
                                        modified.Add(c);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return modified.ToList();
        }




        public void Dispose()
        {
            if (tag != null)
            {
                TagManager.UnregisterTag(tag);
            }
            StarsEffect.Dispose();
        }
    }
}