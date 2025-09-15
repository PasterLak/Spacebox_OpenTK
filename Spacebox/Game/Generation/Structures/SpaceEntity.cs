using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using System.Text;

namespace Spacebox.Game.Generation
{
    public class SpaceEntity : SpatialCell, IDisposable, ISpaceStructure
    {
        public const byte SizeChunks = 16; // will be 16
        public const byte SizeChunksHalf = SizeChunks / 2;
        public const short SizeBlocks = SizeChunks * Chunk.Size;
        public const short SizeBlocksHalf = SizeChunks * Chunk.Size / 2;

        public readonly ulong EntityID;
        public ulong Mass { get; set; } = 0;

        public Octree<Chunk> Octree { get; private set; } // local coords

        private Vector3 sumPosCenterOfMass;
        public Vector3 CenterOfMass { get; private set; }
        public bool IsModified { get; private set; } = false;
        public bool IsGenerated { get; set; } = false;
        public void SetModified() { if (!IsModified) IsModified = true; }
        public Sector Sector { get; private set; }

        public List<Chunk> Chunks { get; private set; } = new List<Chunk>();
        private List<Chunk> MeshesTogenerate = new List<Chunk>();

        private Tag tag;

        private string _entityMassString = "0 tn";
        public BoundingBox GeometryBoundingBox { get; private set; }
        public ElectricNetworkManager ElectricManager { get; private set; }
        public Particle StarParticle;
        public StarsEffect StarsEffect { get; private set; }

        StringBuilder StringBuilder = new StringBuilder();

        public SpaceEntity(ulong id, Vector3 positionWorld, Sector sector)
        {
            EntityID = id;
            Position = positionWorld;
            PositionWorld = positionWorld; 
            Sector = sector;
            BoundingBox = new BoundingBox(positionWorld, new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
            sumPosCenterOfMass = positionWorld;
            Octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);

            GeometryBoundingBox = new BoundingBox(positionWorld, Vector3.Zero);
           
            ElectricManager = new ElectricNetworkManager();

            // BoundingBox.CreateFromMinMax(GeometryMin, GeometryMax)
            tag = CreateTag(positionWorld);
            CalculateCenterOfMass();
            CreateStar();
        }

        public SpaceEntity(Vector3 positionWorld, Sector sector)
        {
            EntityID = 0;
            Position = positionWorld;
            PositionWorld = positionWorld;
            Sector = sector;
            BoundingBox = new BoundingBox(positionWorld, new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
            sumPosCenterOfMass = positionWorld;
            Octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);

            GeometryBoundingBox = new BoundingBox(positionWorld, Vector3.Zero);

           
            // BoundingBox.CreateFromMinMax(GeometryMin, GeometryMax)
            tag = CreateTag(positionWorld);
            CalculateCenterOfMass();
            CreateStar();
        }
        private void CreateStar()
        {

            StarsEffect = new StarsEffect(World.Instance.Player);
           // StarParticle = new Particle(GeometryBoundingBox.Center, Vector3.Zero, 9999999, new Vector4(1, 1, 1, 1), new Vector4(0, 0, 0, 0), 64);
           // StarsEffect.ParticleSystem.AddParticle(StarParticle);
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

                var local = ChunkIndexToLocal(chunk.PositionIndex);

                Octree.Add(chunk, BoundingBox.CreateFromMinMax(local, local + Vector3.One * Chunk.Size));
                Chunks.Add(chunk);

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
            // ElectricManager.UpdateAllNetworks(false, true);
            foreach (var chunk in MeshesTogenerate)
            {
                chunk.GenerateMesh();
            }
            MeshesTogenerate.Clear();
            IsGenerated = true;
        }
        public void AddChunk(Chunk chunk)
        {
            AddChunk(chunk, true);
        }


        public void AddChunk(Chunk chunk, bool generateMesh = true)
        {
            var local = ChunkIndexToLocal(chunk.PositionIndex);

            Octree.Add(chunk, BoundingBox.CreateFromMinMax(local, local + Vector3.One * Chunk.Size));
            Chunks.Add(chunk);

            chunk.OnChunkModified += UpdateEntityGeometryMinMax;
            // if (generateMesh)
            //     chunk.GenerateMesh();
            UpdateNeighbors(chunk);
            RecalculateGeometryBoundingBox();
            RecalculateMass();
        }

        public void RemoveChunk(Chunk chunk)
        {
            Octree.Remove(chunk);
            Chunks.Remove(chunk);

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
                Debug.Error($"[SpaceEntity] Mass was negative! Id: {EntityID}");
            }

            sumPosCenterOfMass = Vector3.Zero;

            for (int i = 0; i < Chunks.Count; i++) // opt
            {
                var chunkMass = Chunks[i].Mass;

                sumPosCenterOfMass += Chunks[i].GetCenterOfMass() * chunkMass;
            }

            CalculateCenterOfMass();
            CalculateGravityRadius();
            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        public void RecalculateMass()
        {
            Mass = 0;
            sumPosCenterOfMass = Vector3.Zero;

            for (int i = 0; i < Chunks.Count; i++)
            {
                var chunkMass = Chunks[i].Mass;
                Mass = (ulong)((long)Mass + chunkMass);

                sumPosCenterOfMass += Chunks[i].GetCenterOfMass() * chunkMass;
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
            tag.TextAlignment = GUI.Tag.Alignment.Right;
            TagManager.RegisterTag(tag);
            Debug.Log($"Tag registered: id {EntityID} pos " + worldPos);
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

        public static Vector3 ChunkIndexToLocal(Vector3SByte chunkIndex)
        {
            var localPos = new Vector3(chunkIndex.X * Chunk.Size, chunkIndex.Y * Chunk.Size, chunkIndex.Z * Chunk.Size);

            return localPos;
        }


        private bool PlaceBlockInternal(Vector3 localPos, Block block)
        {
            int chunkX = (int)MathF.Floor(localPos.X / Chunk.Size);
            int chunkY = (int)MathF.Floor(localPos.Y / Chunk.Size);
            int chunkZ = (int)MathF.Floor(localPos.Z / Chunk.Size);
            Vector3SByte chunkIndex = new Vector3SByte((sbyte)chunkX, (sbyte)chunkY, (sbyte)chunkZ);
            int blockX = (int)localPos.X - chunkX * Chunk.Size;
            int blockY = (int)localPos.Y - chunkY * Chunk.Size;
            int blockZ = (int)localPos.Z - chunkZ * Chunk.Size;
            Vector3Byte blockPos = new Vector3Byte((byte)blockX, (byte)blockY, (byte)blockZ);

            if (Octree.TryFindDataAtPosition(localPos + new Vector3(0.5f, 0.5f, 0.5f), out var chunk))
            {

                chunk.PlaceBlock(blockPos, block);

                if (block.Is<ElectricalBlock>(out var el))
                {
                    // Debug.Log("local " + localPos);
                    ElectricManager.AddBlock(((int)localPos.X, (int)localPos.Y, (int)localPos.Z), el, chunk);
                }

            }
            else
            {
                Chunk newChunk = new Chunk(chunkIndex, this, true);
                AddChunk(newChunk, false);
                newChunk.PlaceBlock(blockPos, block);

                if (block.Is<ElectricalBlock>(out var el))
                {
                    // Debug.Log("local " + localPos);
                    ElectricManager.AddBlock(((int)localPos.X, (int)localPos.Y, (int)localPos.Z), el, newChunk);
                }

            }
            return true;
        }

        public bool TryPlaceBlockLocal(Vector3 localBlockPosition, Block block)
        {
            if (block.Id == 0)
                return true;
            Vector3 worldBlockPos = PositionWorld + localBlockPosition;
            if (!IsPositionWithinEntitySize(worldBlockPos))
                return false;
            return PlaceBlockInternal(localBlockPosition, block);
        }

        public bool TryPlaceBlock(Vector3 worldPosition, Block block)
        {
            if (block.Id == 0)
                return true;
            if (!IsPositionWithinEntitySize(worldPosition) || !IsPositionWithinGravityRadius(worldPosition))
                return false;
            Vector3 localPos = worldPosition - PositionWorld;
            return PlaceBlockInternal(localPos, block);
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

                var index = (chunk.PositionIndex + dir);
                Vector3 neighborCoord = ChunkIndexToLocal(index);

                if (Octree.TryFindDataAtPosition(neighborCoord, out Chunk neighbor))
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
            var local = WorldPositionToLocal(world);

            if (Octree.TryFindDataAtPosition(local, out chunk))
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
        public override void Update()
        {
            base.Update();
            Camera camera = Camera.Main;

            if (camera != null)
            {
                var dis = (int)(Vector3.Distance(CenterOfMass, camera.Position));

               
                if (VisualDebug.Enabled)
                {
                    bool isAsteroid = this as Asteroid != null;
                    StringBuilder.Append(Name)
                      .Append(EntityID)
                      .Append(" isAsteroid: ")
                      .Append(isAsteroid)
                      .Append("\nWpos: ")
                      .Append(Block.RoundVector3(PositionWorld))
                      .Append("\n")
                      .Append(dis )
                      .Append(" m\n")
                      .Append(_entityMassString)
                      .Append(" tn, gR: ")
                      .Append((int)GravityRadius)
                      .Append(" m");
                }
                else
                {
                    StringBuilder.Append(dis ).Append(" m");
                    if (Mass > 0)
                    {
                        StringBuilder.Append("\n")
                          .Append(_entityMassString)
                          .Append(" tn");
                    }
                }

                tag.Text = StringBuilder.ToString();
                StringBuilder.Clear();

                //StarsEffect.Update();

            }

        }

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

                    if (length <= 0) continue;

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

        public bool RemoveBlockAtLocal(Vector3 localBlockPosition, Vector3SByte removalNormal)
        {
            Vector3 worldBlockPos = PositionWorld + localBlockPosition;
            if (!IsPositionWithinEntitySize(worldBlockPos))
            {
                Debug.Error("Local block position is outside the entity boundaries.");
                return false;
            }
            int chunkX = (int)MathF.Floor(localBlockPosition.X / Chunk.Size);
            int chunkY = (int)MathF.Floor(localBlockPosition.Y / Chunk.Size);
            int chunkZ = (int)MathF.Floor(localBlockPosition.Z / Chunk.Size);
            Vector3SByte chunkIndex = new Vector3SByte((sbyte)chunkX, (sbyte)chunkY, (sbyte)chunkZ);
            int blockX = (int)localBlockPosition.X - chunkX * Chunk.Size;
            int blockY = (int)localBlockPosition.Y - chunkY * Chunk.Size;
            int blockZ = (int)localBlockPosition.Z - chunkZ * Chunk.Size;

            if (Octree.TryFindDataAtPosition(localBlockPosition + new Vector3(0.5f, 0.5f, 0.5f), out Chunk chunk))
            {

                var block = chunk.GetBlock((byte)blockX, (byte)blockY, (byte)blockZ);

                if (block is ElectricalBlock)
                {
                    ElectricManager.RemoveBlock(((int)localBlockPosition.X, (int)localBlockPosition.Y, (int)localBlockPosition.Z));
                }

                chunk.RemoveBlock((byte)blockX, (byte)blockY, (byte)blockZ,
                                 removalNormal.X, removalNormal.Y, removalNormal.Z, true);
                return true;
            }
            else
            {
                Debug.Error($"Chunk with index {chunkIndex} not found. Block local was: {localBlockPosition}");
                return false;
            }
        }



        public void RenderEffect(float disSqr)
        {
            return;
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

           // StarParticle.ColorStart = new Vector4(1, 1, 1, alpha);
           // StarParticle.ColorEnd = new Vector4(1, 1, 1, alpha);
           // StarParticle.Size = size;

            StarsEffect.Update();
            StarsEffect.Render();
        }

        HashSet<Chunk> chunks = new HashSet<Chunk>();
        public void Render(Camera camera, BlockMaterial material)
        {

            chunks.Clear();
            Octree.FindDataInRadius(WorldPositionToLocal(camera.Position), Settings.CHUNK_VISIBLE_RADIUS, chunks);

            foreach (var chunk in chunks)
            {

                var dis = (int)(camera.Position - chunk.GeometryBoundingBox.Center).LengthSquared;

                if(dis < Settings.CHUNK_VISIBLE_RADIUS * Settings.CHUNK_VISIBLE_RADIUS)
                {
                    bool visible = chunk.GeometryBoundingBox.Contains(camera.Position);

                    if (!visible)
                    {
                        visible = camera.Frustum.IsInFrustum(chunk.GeometryBoundingBox);
                    }

                    if (!visible) continue;

                    if (chunk.NeedsToRegenerateMesh)
                    {
                        chunk.GenerateMesh(false);

                        chunk.NeedsToRegenerateMesh = false;
                    }

                    chunk.SetLOD(dis);
                    chunk.Render(material);
                }

            }

            if (VisualDebug.Enabled)
            {
                VisualDebug.DrawBoundingBox(BoundingBox, Color4.Cyan);
                VisualDebug.DrawPosition(GeometryBoundingBox.Center, 6, Color4.Orange);

                VisualDebug.DrawBoundingBox(GeometryBoundingBox, Color4.Orange);
                VisualDebug.DrawPosition(CenterOfMass, 8, Color4.Lime);

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
                CenterOfMass = BoundingBox.Center;
                tag.WorldPosition = CenterOfMass;
           
                return;
            }

            CenterOfMass = sumPosCenterOfMass / Mass;
            tag.WorldPosition = CenterOfMass;
         

            // StarParticle.Position = CenterOfMass;
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

                        var localPos = ChunkIndexToLocal(idx);

                        if (!entity.Octree.TryFindDataAtPosition(localPos, out Chunk chunk)) continue;


                        if (chunk == null) continue;

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
                                    Block b = chunk.Blocks[x, y, z];
                                    if (b != null && !b.IsAir)
                                    {
                                        chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
                                        chunk.IsModified = true;
                                        modified.Add(chunk);
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

            var localPos = ChunkIndexToLocal(idx);
            if (!entity.Octree.TryFindDataAtPosition(localPos, out Chunk chunk) || chunk == null)
            {
                chunk = new Chunk(idx, entity, true);

                var local = ChunkIndexToLocal(chunk.PositionIndex);

                entity.Octree.Add(chunk, BoundingBox.CreateFromMinMax(local, local + Vector3.One * Chunk.Size));
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
                                    if (b == null || b.IsAir)
                                    {
                                        c.Blocks[x, y, z] = GameAssets.CreateBlockFromId(blockId);
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
            Sector = null;
            foreach(var mesh in MeshesTogenerate)
            {
                mesh.Dispose();
            }
            foreach (var ch in Chunks)
            {
                ch.Dispose();
            }
            StarsEffect.Dispose();
            if (tag != null)
            {
                //TagManager.UnregisterTag(tag);
            }
            StarsEffect.Dispose();
        }
    }
}