using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player
{
    public class HandItemVisualizer : Node3D
    {
        private readonly Astronaut _owner;
        private ItemWorldModel _itemModel;
        private SimpleBlock _blockModel;

        private bool _isHoldingBlock;
        private bool _isHoldingItem;

        public HandItemVisualizer(Astronaut owner)
        {
            _owner = owner;
            Name = "HandItemVisualizer";
            InitializeModels();
        }

        private void InitializeModels()
        {
            _itemModel = new ItemWorldModel();
            AddChild(_itemModel);

            _itemModel.Rotate(new Vector3(10, 90, -10));
            _itemModel.SetScale(0.6f);
            _itemModel.Position = new Vector3(0.35f, -0.47f, -0.25f);
            _itemModel.Enabled = false;

            _blockModel = new SimpleBlock(new TextureMaterial(GameAssets.BlocksTexture), Vector3.Zero);
            AddChild(_blockModel);

            _blockModel.SetScale(0.2f);
            _blockModel.Rotate(0, 180, 0);
            _blockModel.Position = new Vector3(0.35f, -0.25f, -0.45f);
            _blockModel.Enabled = false;
        }

        public void SetItem(Item? item)
        {

            _isHoldingBlock = false;
            _isHoldingItem = false;

            if (item != null)
            {
                if (item is BlockItem blockItem)
                {
                    PrepareBlock(blockItem);
                    _isHoldingBlock = true;
                }
                else
                {
                    PrepareItem(item);
                    _isHoldingItem = true;
                }
            }

            UpdateVisibilityState();
        }

        private void PrepareItem(Item item)
        {
            _itemModel.ChangeModelTo(item);
            _itemModel.ShowModel = true;
        }

        private void PrepareBlock(BlockItem item)
        {
            var blockData = GameAssets.GetBlockDataById(item.BlockId);
            if (blockData == null) return;

            for (byte i = 0; i < 6; i++)
            {
                Face face = (Face)i;
                Vector2[] uv = blockData.GetFaceUV(face, BlockState.Inactive);
                _blockModel.ChangeUV(uv, face, false);
            }
            _blockModel.RegenerateMesh();
        }

        public override void Render()
        {
            if (!Enabled) return;

            UpdateVisibilityState();

            if (_owner.IsMain) return;

            base.Render();
        }

        private void UpdateVisibilityState()
        {

            if (_owner.IsMain)
            {
                if (_itemModel.Enabled) _itemModel.Enabled = false;
                if (_blockModel.Enabled) _blockModel.Enabled = false;
            }
            else
            {

                if (_itemModel.Enabled != _isHoldingItem) _itemModel.Enabled = _isHoldingItem;
                if (_blockModel.Enabled != _isHoldingBlock) _blockModel.Enabled = _isHoldingBlock;
            }
        }
    }
}