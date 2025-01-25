using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player
{
    public class CreativeTools
    {
        public static BlockPointer Block1 { get; private set; } = null;
        public static BlockPointer Block2 { get; private set; } = null;

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

            Common.Physics.BoundingBox deleteBounding;

            
             deleteBounding = Common.Physics.BoundingBox.CreateFromMinMax(realMin, realMax);


            //Debug.Log($"Delete bounding: {deleteBounding.Min} - {deleteBounding.Max}");


            List<Chunk> chunks = new List<Chunk>();

            if(PanelUI.IsHoldingDrill())
            {
                chunks = SpaceEntity.RemoveBlocksInLocalBox(Block1.spaceEntity, deleteBounding);
            }
            else if(PanelUI.IsHoldingBlock())
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
                chunk.GenerateMesh();
            }
            Reset();
            Debug.Log($"Done");
        }

        private static void Reset()
        {
            Block1 = null;
            Block2 = null;

            Debug.Log($"[CreativeTools] Reset");
        }

    }

    public class BlockPointer
    {
        public short block;
        public Chunk chunk;
        public SpaceEntity spaceEntity;
        public Vector3Byte localBlockIndex;
        public Vector3 localBlockIndexInEntity;

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
        }

        public BlockPointer(HitInfo hitInfo)
        {
            this.block = hitInfo.block.BlockId;
            this.chunk = hitInfo.chunk;
            this.localBlockIndex = hitInfo.blockPositionIndex;
            this.spaceEntity = chunk.SpaceEntity;
            var lPos = chunk.PositionIndex * Chunk.Size;
            localBlockIndexInEntity = new Vector3(lPos.X + localBlockIndex.X, lPos.Y + localBlockIndex.Y, lPos.Z + localBlockIndex.Z);

        }
    }
}
