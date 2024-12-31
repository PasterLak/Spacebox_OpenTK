using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Effects;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.GUI;
using Spacebox.Scenes;

namespace Spacebox.Game.Generation
{
    public class Chunk : IDisposable
    {
        public static Chunk CurrentChunk { get; set; }

        public const byte Size = 32; // 32700+ blocks

        private readonly Dictionary<Vector3SByte, Chunk> Neighbors = new Dictionary<Vector3SByte, Chunk>()
        {
            { new Vector3SByte(1, 0, 0), null },
            { new Vector3SByte(0, 1, 0), null },
            { new Vector3SByte(0, 0, 1), null },
            { new Vector3SByte(-1, 0, 0), null },
            { new Vector3SByte(0, -1, 0), null },
            { new Vector3SByte(0, 0, -1), null },
        };

        public long Mass { get; set; } = 0;
        public Vector3 PositionWorld { get; private set; }
        public Vector3SByte PositionIndex { get; set; }

        public Block[,,] Blocks { get; private set; }
        public bool ShowChunkBounds { get; set; } = true;
        public bool MeasureGenerationTime { get; set; } = true;

        public bool IsModified { get; private set; } = false;
        public bool IsGenerated { get; private set; } = false;

        private Mesh _mesh;
        private readonly MeshGenerator _meshGenerator;
        private readonly LightManager _lightManager;
        private bool _isLoadedOrGenerated = false;

        public SpaceEntity SpaceEntity { get; private set; }
        private BoundingBox boundingBox;
        public BoundingBox GeometryBoundingBox { get; private set; }

        public Action<Chunk> OnChunkModified;

        public Chunk(Vector3 positionWorld)
            : this(positionWorld, null, isLoaded: false)
        {
        }

        internal Chunk(Vector3 positionWorld, Block[,,] loadedBlocks, bool isLoaded)
        {
            PositionWorld = positionWorld;
            Blocks = new Block[Size, Size, Size];

            CurrentChunk = this;
            CreateBoundingBox();
            GeometryBoundingBox = new BoundingBox(boundingBox);


            if (isLoaded && loadedBlocks != null)
            {
                Array.Copy(loadedBlocks, Blocks, loadedBlocks.Length);
                IsGenerated = true;
            }
            else
            {
                BlockGenerator blockGenerator = new BlockGeneratorSphere(Blocks, positionWorld);
                blockGenerator.Generate();
                IsGenerated = true;
            }

            _lightManager = new LightManager(Blocks);
            _lightManager.PropagateLight();

            _meshGenerator = new MeshGenerator(this, Neighbors, MeasureGenerationTime);

            _isLoadedOrGenerated = true;
        }

        private void CreateBoundingBox()
        {
            Vector3 chunkMin = PositionWorld;
            Vector3 chunkMax = PositionWorld + new Vector3(Size);
            boundingBox = BoundingBox.CreateFromMinMax(chunkMin, chunkMax);
        }

        public int GetNeigborCount()
        {
            var count = 0;
            foreach (var n in Neighbors)
            {
                if (n.Value != null) count++;
            }

            return count;
        }

        public void GenerateMesh()
        {
            if (!_isLoadedOrGenerated) return;

            _lightManager.PropagateLight();

            Mesh newMesh = _meshGenerator.GenerateMesh();

            _mesh?.Dispose();
            _mesh = newMesh;

            GeometryBoundingBox = BoundingBox.CreateFromMinMax(
                boundingBox.Min + _meshGenerator.GeometryBoundingBox.Min,
                boundingBox.Min + _meshGenerator.GeometryBoundingBox.Max);

            OnChunkModified?.Invoke(this);
        }

        public bool IsColliding(BoundingVolume volume)
        {
            return VoxelPhysics.IsColliding(volume, Blocks, PositionWorld);
        }

        public void Render(Shader shader)
        {
            if (!_isLoadedOrGenerated) return;

            Vector3 relativePosition = PositionWorld - Camera.Main.Position;

            Vector3 position = Camera.Main.CameraRelativeRender ? relativePosition : PositionWorld;


            Matrix4 model = Matrix4.CreateTranslation(position);
            shader.SetMatrix4("model", model);

            if (_mesh != null)
                _mesh.Draw(shader);

            if (ShowChunkBounds && VisualDebug.ShowDebug)
            {
                VisualDebug.DrawBoundingBox(boundingBox, new Color4(0.5f, 0f, 0.5f, 0.4f));


                //VisualDebug.DrawBoundingBox(GeometryBoundingBox, Color4.Orange);
            }
        }

        public void AddNeighbor(Chunk neighbor)
        {
            Vector3SByte direction = neighbor.PositionIndex - this.PositionIndex;

            Neighbors[direction] = neighbor;
        }

        public void RemoveNeighbor(Chunk neighbor)
        {
            Vector3SByte direction = neighbor.PositionIndex - this.PositionIndex;

            Neighbors[direction] = null;
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

        public void RemoveBlock(Vector3i blockPos, Vector3SByte normal)
        {
            RemoveBlock(blockPos.X, blockPos.Y, blockPos.Z, normal.X, normal.Y, normal.Z);
        }

        public void RemoveBlock(int x, int y, int z, sbyte xNormal, sbyte yNormal, sbyte zNormal)
        {
            if (!IsInRange(x, y, z))
                return;

            Vector3 worldBlockPosition = new Vector3(x, y, z) + PositionWorld;

            if (Blocks[x, y, z].IsTransparent)
            {
                World.DestructionManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor, Blocks[x, y, z]);
                World.DropEffectManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor, Blocks[x, y, z]);
            }
            else
            {
                if (IsInRange(x + xNormal, y + yNormal, z + zNormal))
                {
                    World.DestructionManager.DestroyBlock(worldBlockPosition,
                        Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, Blocks[x, y, z]);
                    World.DropEffectManager.DestroyBlock(worldBlockPosition,
                        Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, Blocks[x, y, z]);
                }
                else
                {
                    World.DestructionManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor,
                        Blocks[x, y, z]);
                    World.DropEffectManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor,
                        Blocks[x, y, z]);
                }
            }


            Mass -= Blocks[x, y, z].Mass;
            Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);

            IsModified = true;
            CheckNeigborBlocks(new Vector3Byte(x, y, z));
            GenerateMesh();
        }

        public Block? GetBlock(Vector3SByte pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        public Block? GetBlock(Vector3 pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        public Block? GetBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
                return Blocks[x, y, z];
            return null;
        }

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }

        private void CheckNeigborBlocks(Vector3Byte pos)
        {
            if (pos.X == 0)
            {
                if (HasNeighbor(new Vector3SByte(-1, 0, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.X == Size - 1)
            {
                if (HasNeighbor(new Vector3SByte(1, 0, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Y == 0)
            {
                if (HasNeighbor(new Vector3SByte(0, -1, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Y == Size - 1)
            {
                if (HasNeighbor(new Vector3SByte(0, 1, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Z == 0)
            {
                if (HasNeighbor(new Vector3SByte(0, 0, -1), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Z == Size - 1)
            {
                if (HasNeighbor(new Vector3SByte(0, 0, 1), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }
        }

        private bool HasNeighbor(Vector3SByte pos, out Chunk neighbor)
        {
            if (Neighbors.TryGetValue(pos, out neighbor))
            {
                return neighbor != null;
            }
            else
            {
                return false;
            }
        }

        public bool Raycast(Ray ray, out VoxelPhysics.HitInfo info)
        {
            info = new VoxelPhysics.HitInfo();

            if (!_isLoadedOrGenerated) return false;

            if (VoxelPhysics.Raycast(ray, PositionWorld, Blocks, out info))
            {
                info.chunk = this;
                return true;
            }

            return false;
        }

        private void ChangeBlockColor(Block block, Vector3 color, bool regenerateMesh)
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

            if (Input.IsKeyDown(Keys.O))
            {
                if (!Debug.IsVisible)
                {
                    _meshGenerator.EnableAO = !_meshGenerator.EnableAO;
                    GenerateMesh();
                }
            }

            if (Input.IsKeyDown(Keys.KeyPad9))
            {
                ClearChunk();
                GenerateMesh();
            }


            var pos = new Vector3Byte((byte)player.Position.X, (byte)player.Position.Y, (byte)player.Position.Z);
            if (IsInRange(pos.X, pos.Y, pos.Z))
            {
                //PanelUI.SetItemColor(Blocks[pos.x, pos.y, pos.z].LightColor.ToSystemVector3());
            }

            Vector3 rayOrigin = player.Position;

            Vector3 rayDirection = player.Front;


            Ray ray = new Ray(rayOrigin, rayDirection, 6);

            VoxelPhysics.HitInfo hitInfo = new VoxelPhysics.HitInfo();

            bool hit = Raycast(ray, out hitInfo);

            if (hit)
            {
                Vector3 worldBlockPosition = hitInfo.blockPosition + PositionWorld;
                VisualDebug.DrawBoundingBox(
                    new BoundingBox(worldBlockPosition + new Vector3(0.5f), Vector3.One * 1.01f), Color4.White);


                Block aimedBlock = Blocks[hitInfo.blockPosition.X, hitInfo.blockPosition.Y, hitInfo.blockPosition.Z];

                VisualDebug.DrawAxes(hitInfo.blockPosition + Vector3.One * 0.5f);

                AImedBlockElement.AimedBlock = aimedBlock;


                if (Input.IsKeyDown(Keys.T))
                {
                    Blocks[hitInfo.blockPosition.X, hitInfo.blockPosition.Y, hitInfo.blockPosition.Z].Color =
                        new Vector3(1, 1, 0);

                    GenerateMesh();
                }

                if (Input.IsKeyDown(Keys.Z))
                {
                    Blocks[hitInfo.blockPosition.X, hitInfo.blockPosition.Y, hitInfo.blockPosition.Z].Color =
                        new Vector3(1, 1, 1);

                    GenerateMesh();
                }

                float dis = Vector3.DistanceSquared(hitInfo.blockPosition + hitInfo.normal, player.Position);

                if (PanelUI.IsHoldingBlock() && dis > Block.DiagonalSquared)
                {
                    BlockSelector.IsVisible = true;
                    BlockSelector.Instance.UpdatePosition(hitInfo.blockPosition + hitInfo.normal,
                        Block.GetDirectionFromNormal(hitInfo.normal));
                }
                else if (PanelUI.IsHoldingDrill())
                {
                    BlockSelector.IsVisible = true;
                    BlockSelector.Instance.UpdatePosition(hitInfo.blockPosition,
                        Block.GetDirectionFromNormal(hitInfo.normal));
                }
                else
                {
                    BlockSelector.IsVisible = false;
                }


                VisualDebug.DrawBoundingSphere(new BoundingSphere(hitInfo.position, 0.02f), Color4.Red);
                VisualDebug.DrawLine(hitInfo.position, hitInfo.position + hitInfo.normal * 0.5f, Color4.Red);


                if (aimedBlock.GetType() == typeof(InteractiveBlock))
                {
                    float dis2 = Vector3.Distance(player.Position, hitInfo.blockPosition);

                    if (dis2 < 3f)
                    {
                        CenteredText.Show();

                        if (Input.IsKeyDown(Keys.F))
                        {
                            ((InteractiveBlock)aimedBlock).Use();
                        }
                    }

                    else
                        CenteredText.Hide();
                }
                else
                {
                    CenteredText.Hide();
                }

                if (Input.IsMouseButtonDown(MouseButton.Middle))
                {
                    if (!GameBlocks.GetBlockDataById(aimedBlock.BlockId).AllSidesAreSame)
                    {
                        aimedBlock.SetDirectionFromNormal(hitInfo.normal);
                        GenerateMesh();
                    }
                }


                if (Input.IsMouseButtonDown(MouseButton.Left))
                {
                    if (PanelUI.IsHoldingDrill())
                    {
                        if (!InventoryUI.IsVisible && !Debug.IsVisible)
                        {
                            // player.Panel.TryAddItem(
                            //  GameBlocks.GetBlockDataById(aimedBlock.BlockId).Item, 1);


                            RemoveBlock(hitInfo.blockPosition.X, hitInfo.blockPosition.Y, hitInfo.blockPosition.Z,
                                (sbyte)hitInfo.normal.X, (sbyte)hitInfo.normal.Y, (sbyte)hitInfo.normal.Z);

                            SpaceScene.blockDestroy.Play();
                        }
                    }
                }


                if (Input.IsMouseButtonDown(MouseButton.Right))
                {
                    Vector3i placeBlockPosition = hitInfo.blockPosition + new Vector3i((int)hitInfo.normal.X,
                        (int)hitInfo.normal.Y, (int)hitInfo.normal.Z);

                    if (Vector3.DistanceSquared(player.Position, hitInfo.blockPosition + hitInfo.normal) >
                        Block.DiagonalSquared)
                    {
                        if (IsInRange(placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z) &&
                            Blocks[placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z].IsAir())
                        {
                            if (PanelUI.TryPlaceItem(out var id))
                            {
                                Block newBlock = GameBlocks.CreateBlockFromId(id);

                                bool hasSameSides = GameBlocks.GetBlockDataById(id).AllSidesAreSame;

                                if (!hasSameSides)
                                    newBlock.SetDirectionFromNormal(hitInfo.normal);

                                SetBlock(placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z, newBlock);

                                SpaceScene.blockPlace.Play();
                            }
                        }
                    }
                }
            }
            else
            {
                if (BlockSelector.IsVisible)
                    BlockSelector.IsVisible = false;

                AImedBlockElement.AimedBlock = null;

                float placeDistance = 5f;
                Vector3 placePosition = rayOrigin + rayDirection * placeDistance;
                Vector3 localPosition = placePosition - PositionWorld;

                int x = (int)MathF.Floor(localPosition.X);
                int y = (int)MathF.Floor(localPosition.Y);
                int z = (int)MathF.Floor(localPosition.Z);

                Vector3 worldBlockPosition = new Vector3(x, y, z) + PositionWorld;
                VisualDebug.DrawBoundingBox(
                    new BoundingBox(worldBlockPosition + new Vector3(0.5f), Vector3.One * 1.01f), Color4.Gray);


                float dis = Vector3.DistanceSquared(worldBlockPosition, player.Position);

                if (PanelUI.IsHoldingBlock() && dis > Block.DiagonalSquared)
                {
                    //Debug.Log("holding");
                    var norm = (player.Position - worldBlockPosition).Normalized();

                    norm = Block.RoundVector3(norm);

                    BlockSelector.IsVisible = true;

                    var direction = Block.GetDirectionFromNormal(norm);
                    BlockSelector.Instance.UpdatePosition(worldBlockPosition, direction);


                    if (Input.IsMouseButtonDown(MouseButton.Right) &&
                        IsInRange(x, y, z) &&
                        Blocks[x, y, z].IsAir())
                    {
                        if (PanelUI.TryPlaceItem(out var blockId))
                        {
                            Block newBlock = GameBlocks.CreateBlockFromId(blockId);

                            bool hasSameSides = GameBlocks.GetBlockDataById(blockId).AllSidesAreSame;

                            if (!hasSameSides)
                                newBlock.Direction = direction;

                            SetBlock(x, y, z, newBlock);

                            SpaceScene.blockPlace.Play();
                        }
                        else
                        {
                        }
                    }
                }
                else
                {
                    BlockSelector.IsVisible = false;
                }

                if (CenteredText.IsVisible)
                {
                    CenteredText.Hide();
                }
            }
        }

        private void ClearChunk()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        Blocks[x, y, z] = new Block(new Vector2(0, 0));
                    }
                }
            }
        }

        public void Dispose()
        {
            _mesh?.Dispose();
            CurrentChunk = null;
        }
    }
}