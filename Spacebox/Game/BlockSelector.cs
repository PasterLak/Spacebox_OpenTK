using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Game
{
    public class BlockSelector : IDisposable
    {
        public static BlockSelector Instance;
        public static bool IsVisible = false;

        private Texture2D selectorTexture;
        public SimpleBlock SimpleBlock { get; private set; }

        private Direction blockDirection = Direction.Up;
        public Rotation Rotation = Rotation.None;
        private BlockData currentBlockData;

        public BlockSelector()
        {
            Instance = this;
            selectorTexture = Resources.Load<Texture2D>("Resources/Textures/selector.png");
            selectorTexture.FilterMode = FilterMode.Nearest;

            var material = new TextureMaterial(selectorTexture, Resources.Load<Shader>("Shaders/blockPreview"));
            material.Color = new Color4(1f, 1f, 1f, 1f);

            SimpleBlock = new SimpleBlock(material, Vector3.Zero);
            SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);

            PanelUI.OnSlotChanged += OnSelectedSlotWasChanged;
            OnSelectedSlotWasChanged(PanelUI.SelectedSlotId);
        }

        public void OnSelectedSlotWasChanged(short slot)
        {
            Rotation = Rotation.None;
            SimpleBlock.ResetBlockTransform();

            if (PanelUI.IsHolding<DrillItem>())
            {
                SimpleBlock.Material.MainTexture = selectorTexture;
                SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);
                SimpleBlock.ResetUV();
                currentBlockData = null;
            }
            else if (PanelUI.IsHolding<BlockItem>())
            {
                var blockItem = PanelUI.CurrentSlot().Item as BlockItem;
                if (blockItem != null)
                {
                    currentBlockData = GameAssets.GetBlockDataById(blockItem.Id);
                    SimpleBlock.Material.MainTexture = GameAssets.BlocksTexture;
                    SimpleBlock.Scale = new Vector3(1f, 1f, 1f);
                    SetupBlockMesh();
                    UpdateBlockRotation();
                }
            }
        }

        private void SetupBlockMesh()
        {
            if (currentBlockData == null) return;

            for (byte i = 0; i < 6; i++)
            {
                Face face = (Face)i;
                Vector2[] uv = currentBlockData.GetFaceUV(face);
                SimpleBlock.ChangeUV(uv, face, false);
            }

            SimpleBlock.RegenerateMesh();
        }

        private void UpdateBlockRotation()
        {
            if (currentBlockData == null) return;

            var transformMatrix = BlockRotationHelper.CalculateTransformMatrix(
                currentBlockData.BaseFrontDirection,
                blockDirection,
                Rotation
            );

            SimpleBlock.SetBlockTransform(transformMatrix);
        }

        public void UpdatePosition(Vector3 position, Direction direction)
        {
            Vector3 newPosition = position + Vector3.One * 0.5f;

            if (SimpleBlock.Position == newPosition && blockDirection == direction)
                return;

            SimpleBlock.Position = newPosition;

            if (blockDirection != direction)
            {
                blockDirection = direction;
                if (currentBlockData != null)
                {
                    UpdateBlockRotation();
                }
            }
        }

        public void Render()
        {
            if (!IsVisible || !Settings.ShowInterface) return;

            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.R))
            {
                Rotation = (Rotation)(((byte)Rotation + 1) % 4);
                if (currentBlockData != null)
                {
                    UpdateBlockRotation();
                }
            }

            SimpleBlock.Render();
        }

        public Direction GetDirection() => blockDirection;
        public Rotation GetRotation() => Rotation;

        public void Dispose()
        {
            SimpleBlock?.Dispose();
            Instance = null;
        }
    }
}