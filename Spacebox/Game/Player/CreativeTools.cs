using OpenTK.Mathematics;

using Engine.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Engine;
namespace Spacebox.Game.Player
{
    public class CreativeTools
    {
        public static BlockPointer Block1 { get; private set; } = null;
        public static BlockPointer Block2 { get; private set; } = null;


        public static void SetPoint1(BlockPointer blockPointer)
        {
            Block1 = blockPointer;
        }
        public static void SetPoint2(BlockPointer blockPointer)
        {
            Block2 = blockPointer;
        }
        public static void AddBlock(BlockPointer blockPointer)
        {
            if(Block1 == null)
            {
                Block1 = blockPointer;
                Debug.Log($"[CreativeTools] Block1 was selected");
                return;
            }
            else
            {
                if(Block2 != null)
                {
                    Block1 = Block2;
                    Block2 = blockPointer;
                    Debug.Log($"[CreativeTools] Block2 was selected");
                }
                else
                {
                    Block2 = blockPointer;
                    Debug.Log($"[CreativeTools] Block2 was selected");
                }

            }
        }

        public static BoundingBox? GetBoundingBox()
        {
            if (Block1 == null && Block2 == null) return null;

            if(Block1 != null && Block2 == null)
            {
                return CreateBoundingBoxForTwoBlocks(Block1.worldPosition, Block1.worldPosition);
            }
            if (Block1 != null && Block2 != null)
            {
                return CreateBoundingBoxForTwoBlocks(Block1.worldPosition, Block2.worldPosition);
            }

            return null;

        }

        public static BoundingBox CreateBoundingBoxForTwoBlocks(Vector3 block1Min, Vector3 block2Min, Vector3 blockSize = default)
        {
       
            if (blockSize == default)
            {
                blockSize = Vector3.One;
            }

         
            Vector3 block1Max = block1Min + blockSize;
            Vector3 block2Max = block2Min + blockSize;

       
            Vector3 overallMin = Vector3.ComponentMin(block1Min, block2Min);
            Vector3 overallMax = Vector3.ComponentMax(block1Max, block2Max);

            return BoundingBox.CreateFromMinMax(overallMin, overallMax);
        }

        public static void DeleteBlocks()
        {
            if (Block1 == null || Block2 == null)
            {
                Reset();
                return;
            }

            if (Block1.spaceEntity == null || Block2.spaceEntity == null)
            {
                Reset();
                return;
            }


            var local1 = Block1.localBlockIndexInEntity;
            var local2 = Block2.localBlockIndexInEntity;

            if (local1 == local2)
            {
                Reset();
                return;
            }

            Vector3 realMin = Vector3.ComponentMin((Vector3)local1, (Vector3)local2);
            Vector3 realMax = Vector3.ComponentMax((Vector3)local1, (Vector3)local2);

            Engine.Physics.BoundingBox deleteBounding;

            
             deleteBounding = Engine.Physics.BoundingBox.CreateFromMinMax(realMin, realMax);


            //Debug.Log($"Delete bounding: {deleteBounding.Min} - {deleteBounding.Max}");


            List<Chunk> chunks = new List<Chunk>();

            if(PanelUI.IsHolding<EraserToolItem>())
            {
                chunks = SpaceEntity.RemoveBlocksInLocalBox(Block1.spaceEntity, deleteBounding);
            }
            else if(PanelUI.IsHolding< BlockItem>())
            {
                var slot = PanelUI.CurrentSlot();

                if(slot.HasItem)
                {
                    chunks = SpaceEntity.FillBlocksInLocalBox(Block1.spaceEntity, deleteBounding, slot.Item.Id);
                }
                
            }
            else
            {
                Reset();
                return;
            }

            foreach ( var chunk in chunks )
            {
                chunk.MarkNeedsRegenerate();
            }
            Reset();
            
        }

        public static void Reset()
        {
            Block1 = null;
            Block2 = null;

          
        }

    }

    public class BlockPointer
    {
        public short block;
        public Chunk chunk;
        public SpaceEntity spaceEntity;
        public Vector3Byte localBlockIndex;
        public Vector3 localBlockIndexInEntity;
        public Vector3 worldPosition;

        public BlockPointer(short block, Chunk chunk, Vector3Byte localBlockIndex)
        {
            this.block = block;
            this.chunk = chunk;
            this.localBlockIndex = localBlockIndex;
            this.spaceEntity = chunk.SpaceEntity;
            var lPos = chunk.PositionIndex * Chunk.Size;
            localBlockIndexInEntity = new Vector3(lPos.X + localBlockIndex.X, lPos.Y + localBlockIndex.Y, lPos.Z + localBlockIndex.Z);
        }

        public BlockPointer(short block, SpaceEntity entity, Vector3 localBlockIndexInEntity)
        {
            this.block = block;
            this.chunk = null;
            this.localBlockIndex = localBlockIndex;
            this.spaceEntity = entity;
            // var lPos = chunk.PositionIndex * Chunk.Size;
            this.localBlockIndexInEntity = localBlockIndexInEntity;
            worldPosition = entity.LocalPositionToWorld(localBlockIndexInEntity);

        }

        public BlockPointer(HitInfo hitInfo)
        {
            this.block = hitInfo.block.BlockId;
            this.chunk = hitInfo.chunk;
            this.localBlockIndex = hitInfo.blockPositionIndex;
            this.spaceEntity = chunk.SpaceEntity;
            var lPos = chunk.PositionIndex * Chunk.Size;
            localBlockIndexInEntity = new Vector3(lPos.X + localBlockIndex.X, lPos.Y + localBlockIndex.Y, lPos.Z + localBlockIndex.Z);

            worldPosition = this.spaceEntity.LocalPositionToWorld(localBlockIndexInEntity);
        }
    }
}
