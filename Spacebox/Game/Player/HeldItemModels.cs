using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player;

public readonly record struct HeldItemPose(Vector3 ItemPosition, float ItemScale, Vector3 BlockPosition, float BlockScale)
{
    public static readonly HeldItemPose Hand = new HeldItemPose(new Vector3(0.35f, -0.47f, -0.25f), 0.6f, new Vector3(0.35f, -0.25f, -0.45f), 0.2f);
}

public sealed class HeldItemModels
{
    private readonly HeldItemPose _pose;

    public ItemWorldModel ItemModel { get; }
    public SimpleBlock BlockModel { get; }
    public bool HoldingItem { get; private set; }
    public bool HoldingBlock { get; private set; }

    public HeldItemModels(Node3D parent) : this(parent, HeldItemPose.Hand)
    {
    }

    public HeldItemModels(Node3D parent, HeldItemPose pose)
    {
        _pose = pose;
        ItemModel = new ItemWorldModel();
        parent.AddChild(ItemModel);
        ItemModel.Rotate(new Vector3(10, 90, -10));
        ItemModel.SetScale(pose.ItemScale);
        ItemModel.Position = pose.ItemPosition;
        ItemModel.Enabled = false;

        BlockModel = new SimpleBlock(new TextureMaterial(GameAssets.BlocksTexture), Vector3.Zero) { DisposesTexture = false };
        parent.AddChild(BlockModel);
        BlockModel.SetScale(pose.BlockScale);
        BlockModel.Rotate(0, 180, 0);
        BlockModel.Position = pose.BlockPosition;
        BlockModel.Enabled = false;
    }

    public void SetItem(Item? item)
    {
        HoldingBlock = false;
        HoldingItem = false;
        if (item == null) return;

        if (item is BlockItem blockItem && blockItem.BlockId != 0)
        {
            HoldingBlock = PrepareBlock(blockItem);
            return;
        }

        ItemModel.ChangeModelTo(item);
        ItemModel.ShowModel = true;
        HoldingItem = true;
    }

    public void Destroy()
    {
        BlockModel.Destroy();
        HoldingBlock = false;
        HoldingItem = false;
    }

    public void SetOffset(Vector3 offset)
    {
        ItemModel.Position = _pose.ItemPosition + offset;
        BlockModel.Position = _pose.BlockPosition + offset;
    }

    public void SetVisible(bool visible)
    {
        bool item = visible && HoldingItem;
        bool block = visible && HoldingBlock;
        if (ItemModel.Enabled != item) ItemModel.Enabled = item;
        if (BlockModel.Enabled != block) BlockModel.Enabled = block;
    }

    private bool PrepareBlock(BlockItem item)
    {
        var blockData = GameAssets.GetBlockDataById(item.BlockId);
        if (blockData == null) return false;

        if (HeldBlockMesh.TryBuild(blockData, out var vertices, out var indices))
        {
            BlockModel.SetCustomMesh(vertices, indices);
            return true;
        }

        for (byte i = 0; i < 6; i++)
        {
            Face face = (Face)i;
            Vector2[] uv = blockData.GetFaceUV(face, BlockState.Inactive);
            BlockModel.ChangeUV(uv, face, false);
        }
        BlockModel.RegenerateMesh();
        return true;
    }
}
