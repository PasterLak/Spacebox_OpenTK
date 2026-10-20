using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Engine;
using Spacebox.Game.Generation.Tools;
using Spacebox.Network;

namespace Spacebox.Game.Player
{
    public class CreativeTools
    {
        public static BlockPointer Block1 { get; private set; } = null;
        public static BlockPointer Block2 { get; private set; } = null;

        public static void SetPoint1(BlockPointer blockPointer)
        {
            Block1 = blockPointer;
            if (Block2 != null && Block2.spaceEntity != blockPointer?.spaceEntity) Block2 = null;
        }

        public static void SetPoint2(BlockPointer blockPointer)
        {
            Block2 = blockPointer;
            if (Block1 != null && Block1.spaceEntity != blockPointer?.spaceEntity) Block1 = null;
        }

        public static void AddBlock(BlockPointer blockPointer)
        {
            if (Block1 == null)
            {
                Block1 = blockPointer;
                Debug.Log($"[CreativeTools] Block1 was selected");
                return;
            }
            else
            {
                if (Block2 != null)
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

        public static bool TryGetSelection(out SpaceEntity entity, out Vector3i min, out Vector3i max)
        {
            var first = Block1 ?? Block2;
            var second = Block1 != null && Block2 != null && Block2.spaceEntity == Block1.spaceEntity ? Block2 : first;

            entity = first?.spaceEntity;
            min = default;
            max = default;
            if (entity == null) return false;

            min = Vector3i.ComponentMin(first.Cell, second.Cell);
            max = Vector3i.ComponentMax(first.Cell, second.Cell);
            return true;
        }

        public static void DeleteBlocks()
        {
            if (Block1?.spaceEntity == null || Block2?.spaceEntity == null || Block1.spaceEntity != Block2.spaceEntity)
            {
                Reset();
                return;
            }

            short blockId;
            if (PanelUI.IsHolding<EraserToolItem>())
            {
                blockId = BulkBlockEdit.EraseBlockId;
            }
            else if (PanelUI.IsHolding<BlockItem>() && PanelUI.CurrentSlot().HasItem)
            {
                blockId = PanelUI.CurrentSlot().Item.Id;
            }
            else
            {
                Reset();
                return;
            }

            if (Spacebox.Game.Protection.EntityProtection.CheckLocalEditBox(Block1.spaceEntity, Vector3i.ComponentMin(Block1.Cell, Block2.Cell), Vector3i.ComponentMax(Block1.Cell, Block2.Cell))) WorldEditNetwork.RequestBulkEdit(Block1.spaceEntity, Block1.Cell, Block2.Cell, blockId);
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

        public Vector3i Cell => new Vector3i(
            (int)MathF.Floor(localBlockIndexInEntity.X),
            (int)MathF.Floor(localBlockIndexInEntity.Y),
            (int)MathF.Floor(localBlockIndexInEntity.Z));

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
            this.localBlockIndex = default;
            this.spaceEntity = entity;
            this.localBlockIndexInEntity = localBlockIndexInEntity;
        }

        public BlockPointer(short block, SpaceEntity entity, Vector3i cell)
            : this(block, entity, new Vector3(cell.X, cell.Y, cell.Z))
        {
        }

        public BlockPointer(HitInfo hitInfo)
        {
            this.block = hitInfo.block.Id;
            this.chunk = hitInfo.chunk;
            this.localBlockIndex = hitInfo.blockPositionIndex;
            this.spaceEntity = chunk.SpaceEntity;
            var lPos = chunk.PositionIndex * Chunk.Size;
            localBlockIndexInEntity = new Vector3(lPos.X + localBlockIndex.X, lPos.Y + localBlockIndex.Y, lPos.Z + localBlockIndex.Z);
        }
    }
}
