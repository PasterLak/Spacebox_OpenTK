
using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game
{
    public class BlockSelector : IDisposable
    {

        public static BlockSelector Instance;
        public static bool IsVisible = false;


        private Texture2D selectorTexture;

        public SimpleBlock SimpleBlock { get; private set; }
        private CubeRenderer CubeRenderer { get; }

        private Texture2D currentTexture;

        private Direction blockDirection = Direction.Up;
        public BlockSelector() 
        {
            Instance = this;
           
            selectorTexture = Resources.Load<Texture2D>("Resources/Textures/selector.png");

            selectorTexture.FilterMode = FilterMode.Nearest;



            SimpleBlock = new SimpleBlock(new TextureMaterial(selectorTexture), Vector3.Zero);
            SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);

            CubeRenderer = new CubeRenderer(Vector3.Zero, TextureMaterial.GetMeshBuffer());
            CubeRenderer.Material = new TextureMaterial(selectorTexture);
          
            CubeRenderer.Scale = new Vector3(1.05f, 1.05f, 1.05f);
            CubeRenderer.Color = Color4.White;

            currentTexture = selectorTexture;
            PanelUI.OnSlotChanged += OnSelectedSlotWasChanged;
            //block.ChangeUV(UVAtlas.GetUVs(3,3));

            OnSelectedSlotWasChanged(PanelUI.SelectedSlotId);
        }

        public void OnSelectedSlotWasChanged(short slot)
        {

            if(PanelUI.IsHolding< DrillItem>())
            {
              
                if (!SimpleBlock.IsUsingDefaultUV)
                {
                    SimpleBlock.Material.MainTexture = selectorTexture;
                    SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);
                    SimpleBlock.ResetUV();
                }
                
            }

            else if (PanelUI.IsHolding< BlockItem>())
            {
               
                //if (block.IsUsingDefaultUV)   // to do block was changed
                //{
                    SimpleBlock.Material.MainTexture = GameAssets.BlocksTexture;
                SimpleBlock.Scale = new Vector3(1,1,1);
                UpdateUV();
                // }


            }
           

        }

        private void UpdateUV()
        {
            SimpleBlock.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Left, blockDirection), Face.Left, false);
            SimpleBlock.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Right, blockDirection), Face.Right, false);
            SimpleBlock.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Back, blockDirection), Face.Back, false);
            SimpleBlock.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Front, blockDirection), Face.Front, false);
            SimpleBlock.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Top, blockDirection), Face.Top, false);
            SimpleBlock.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Bottom, blockDirection), Face.Bottom, false);


            SimpleBlock.RegenerateMesh();
        }

        public void UpdatePosition(Vector3 position, Direction direction)
        {
            if(SimpleBlock.Position == position) return;

            position += Vector3.One * 0.5f;

           
            if (!SimpleBlock.IsUsingDefaultUV)
            {
                if (blockDirection != direction)
                {
                    blockDirection = direction;

                    UpdateUV();
                }
            }
            


            SimpleBlock.Position = position;
            CubeRenderer.Position = position;
        }


        public void Render(Astronaut camera)
        {
          
            if(!IsVisible) return;
            if(!Settings.ShowInterface) return;


           // CubeRenderer.Render();

            //GL.Enable(EnableCap.DepthTest);
            //blockModel.Draw(camera);
            SimpleBlock.Render(camera);

            //GL.Disable(EnableCap.DepthTest);


        }

        public void Dispose()
        {
            SimpleBlock.Dispose();
            CubeRenderer.Dispose();
            Instance = null;
        }
    }
}
