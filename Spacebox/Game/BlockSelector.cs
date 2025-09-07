using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
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
            if (PanelUI.IsHolding<DrillItem>())
            {
                if (!SimpleBlock.IsUsingDefaultUV)
                {
                    SimpleBlock.Material.MainTexture = selectorTexture;
                    SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);
                    SimpleBlock.ResetUV();
                }
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
                    UpdateUV();
                }
            }
        }

        private void UpdateUV()
        {
            if (currentBlockData == null) return;

            for (byte i = 0; i < 6; i++)
            {
                Face worldFace = (Face)i;
                Vector2[] uv = CalculateUVForFace(worldFace);
                SimpleBlock.ChangeUV(uv, worldFace, false);
            }

            SimpleBlock.RegenerateMesh();
        }

        private Vector2[] CalculateUVForFace(Face worldFace)
        {
            Face blockFace = BlockRotationTable.GetBlockFaceForWorld(worldFace, blockDirection, Rotation);
            Vector2[] baseUV = currentBlockData.GetFaceUV(blockFace);
            
            byte uvRotation = BlockRotationTable.GetCombinedUVRotation(worldFace, blockDirection, Rotation);

            return uvRotation switch
            {
                1 => BlockData.RotateUV90Right(baseUV),
                2 => BlockData.RotateUV180(baseUV),
                3 => BlockData.RotateUV90Left(baseUV),
                _ => baseUV
            };
        }

        public void UpdatePosition(Vector3 position, Direction direction)
        {
            if (SimpleBlock.Position == position && blockDirection == direction)
                return;

            position += Vector3.One * 0.5f;
            SimpleBlock.Position = position;

            if (!SimpleBlock.IsUsingDefaultUV && blockDirection != direction)
            {
                blockDirection = direction;
                UpdateUV();
            }
        }
    

        public void Render()
        {
            if (!IsVisible) return;
            if (!Settings.ShowInterface) return;

            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.R))
            {
                Rotation = (Rotation)(((byte)Rotation + 1) % 4);
                Debug.Log(Rotation);
                UpdateUV();
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