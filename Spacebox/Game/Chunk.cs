using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Lighting;
using Spacebox.Game.Physics;
using Spacebox.Game.Rendering;
using Spacebox.GUI;
using Spacebox.Managers;
using Spacebox.Scenes;
using Spacebox.UI;

namespace Spacebox.Game
{
    public class Chunk : IDisposable
    {
        public static Chunk CurrentChunk { get; set; }

        public const byte Size = 32; // 32700+ blocks

        public long Mass { get; set; } = 0;
        public Vector3 Position { get; private set; }
        public Block[,,] Blocks { get; private set; }
        public bool ShowChunkBounds { get; set; } = true;
        public bool MeasureGenerationTime { get; set; } = true;
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

        internal Chunk(Vector3 position, Block[,,] loadedBlocks, bool isLoaded)
        {
            Position = position;
            Blocks = new Block[Size, Size, Size];
            if (CurrentChunk != null) Debug.Error("[Chunk] Many chunks");
            CurrentChunk = this;
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

        public bool IsColliding(BoundingVolume volume)
        {

            return VoxelPhysics.IsColliding(volume, Blocks, Position);
            
        }

        public void Draw(Shader shader)
        {
            if (!_isLoadedOrGenerated) return;

                Vector3 relativePosition = Position - Camera.Main.Position;

                Vector3 position = Camera.Main.CameraRelativeRender ? relativePosition : Position;


                Matrix4 model = Matrix4.CreateTranslation(position);
                shader.SetMatrix4("model", model);
                _mesh.Draw(shader);

                if (ShowChunkBounds && VisualDebug.ShowDebug)
                {

                    VisualDebug.DrawBoundingBox(boundingBox, new Color4(0.5f, 0f, 0.5f, 1f));
                }

                destructionManager.Render();


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

     
        public void RemoveBlock(int x, int y, int z, sbyte xNormal, sbyte yNormal, sbyte zNormal)
        {
            if (!IsInRange(x, y, z))
                return;
            
            Vector3 worldBlockPosition = new Vector3(x, y, z);

            if(Blocks[x, y, z].IsTransparent)
            {
                destructionManager?.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor, Blocks[x, y, z]);
                World.DropEffectManager.
                    DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor, Blocks[x, y, z]);
            }
            else
            {
                if(IsInRange(x + xNormal, y + yNormal, z + zNormal))
                {
                  
                    destructionManager?.DestroyBlock(worldBlockPosition, Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, Blocks[x, y, z]);
                    World.DropEffectManager.
                       DestroyBlock(worldBlockPosition, Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, Blocks[x, y, z]);
                }
                else
                {
                    destructionManager?.
                        DestroyBlock(worldBlockPosition, Blocks[x, y , z ].LightColor, Blocks[x, y, z]);
                    World.DropEffectManager.
                   DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor, Blocks[x, y, z]);
                }
            }

            Mass -= Blocks[x, y, z].Mass;
            Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);

            

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
            return GameBlocks.CreateBlockFromId(0);
        }

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }

        public bool Raycast(Ray ray, out VoxelPhysics.HitInfo info)
        {
            info = new VoxelPhysics.HitInfo();  

            if (!_isLoadedOrGenerated) return false;

            return VoxelPhysics.Raycast(ray,Position, Blocks, out info);
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

            if (Input.IsKeyDown(Keys.O))
            {
                if (!Debug.IsVisible)
                {
                    _meshGenerator.EnableAO = !_meshGenerator.EnableAO;
                    GenerateMesh();
                }
                    
            }

            if (Input.IsKeyDown(Keys.P))
            {
                if(!Debug.IsVisible )
                ChunkSaveLoadManager.SaveChunk(this, World.Instance.WorldData.GetName());
            }

            if(Input.IsKeyDown(Keys.KeyPad9))
            {
                ClearChunk();
                GenerateMesh();
            }
           
            tag.Text =  (int)Vector3.Distance(boundingBox.Center, player.Position) + " m";
                destructionManager.Update();

            var pos = new Vector3Byte((byte)player.Position.X, (byte)player.Position.Y, (byte)player.Position.Z); 
            if(IsInRange(pos.X,pos.Y,pos.Z))
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

                Vector3 worldBlockPosition = hitInfo.blockPosition + Position;
                VisualDebug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f), Vector3.One * 1.01f), Color4.White);


                Block aimedBlock = Blocks[hitInfo.blockPosition.X, hitInfo.blockPosition.Y, hitInfo.blockPosition.Z];

                VisualDebug.DrawAxes(hitInfo.blockPosition + Vector3.One * 0.5f);

                Overlay.AimedBlock = aimedBlock;

               
                if (Input.IsKeyDown(Keys.T))
                {
                    Blocks[hitInfo.blockPosition.X, hitInfo.blockPosition.Y, hitInfo.blockPosition.Z].Color = new Vector3(1, 1, 0);

                    GenerateMesh();
                }
                if (Input.IsKeyDown(Keys.Z))
                {
                    Blocks[hitInfo.blockPosition.X, hitInfo.blockPosition.Y, hitInfo.blockPosition.Z].Color = new Vector3(1, 1, 1);

                    GenerateMesh();
                }

                float dis = Vector3.DistanceSquared(hitInfo.blockPosition + hitInfo.normal, player.Position);

                if (PanelUI.IsHoldingBlock() && dis > Block.DiagonalSquared)
                {
                    BlockSelector.IsVisible = true;
                    BlockSelector.Instance.UpdatePosition(hitInfo.blockPosition + hitInfo.normal, Block.GetDirectionFromNormal(hitInfo.normal));
                }
                else if (PanelUI.IsHoldingDrill())
                {
                    BlockSelector.IsVisible = true;
                    BlockSelector.Instance.UpdatePosition(hitInfo.blockPosition, Block.GetDirectionFromNormal(hitInfo.normal));
                }
                else
                {
                    BlockSelector.IsVisible = false;
                }


                VisualDebug.DrawBoundingSphere(new BoundingSphere(hitInfo.position, 0.02f), Color4.Red);
                VisualDebug.DrawLine(hitInfo.position, hitInfo.position + hitInfo.normal * 0.5f, Color4.Red);


                if (aimedBlock.GetType() == typeof(InteractiveBlock) )
                {
                    float dis2 = Vector3.Distance(player.Position, hitInfo.blockPosition);

                    if (dis2 < 3f)
                    {
                        CenteredText.Show();

                        if(Input.IsKeyDown(Keys.F))
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
                    if(!GameBlocks.GetBlockDataById(aimedBlock.BlockId).AllSidesAreSame)
                    {
                        aimedBlock.SetDirectionFromNormal(hitInfo.normal);
                        GenerateMesh();
                    }
                    
                }
                    

                 if (Input.IsMouseButtonDown(MouseButton.Left))
                 {
                    if(PanelUI.IsHoldingDrill())
                    {
                        if(!InventoryUI.IsVisible && !Debug.IsVisible)
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
                    Vector3i placeBlockPosition = hitInfo.blockPosition + new Vector3i((int)hitInfo.normal.X, (int)hitInfo.normal.Y, (int)hitInfo.normal.Z);

                    if(Vector3.DistanceSquared(player.Position, hitInfo.blockPosition + hitInfo.normal) > Block.DiagonalSquared)
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
                if(BlockSelector.IsVisible)
                BlockSelector.IsVisible = false;

                Overlay.AimedBlock = null;

                float placeDistance = 5f;
                Vector3 placePosition = rayOrigin + rayDirection * placeDistance;
                Vector3 localPosition = placePosition - Position;

                int x = (int)MathF.Floor(localPosition.X);
                int y = (int)MathF.Floor(localPosition.Y);
                int z = (int)MathF.Floor(localPosition.Z);

                Vector3 worldBlockPosition = new Vector3(x, y, z) + Position;
                Spacebox.Common.VisualDebug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f), Vector3.One * 1.01f), Color4.Gray);


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

                            if(!hasSameSides)
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
            for(int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);
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
