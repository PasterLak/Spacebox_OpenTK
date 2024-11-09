
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.UI;


namespace Spacebox.Game
{
    public class BlockSelector
    {

        public static BlockSelector Instance;
        public static bool IsVisible = false;


        private Texture2D selectorTexture;

        private SimpleBlock block;
        private Texture2D currentTexture;

        private Direction blockDirection = Direction.Up;
        public BlockSelector() 
        {
            Instance = this;
            Shader shader = ShaderManager.GetShader("Shaders/textured");
            selectorTexture = TextureManager.GetTexture("Resources/Textures/selector.png", true);

          

            block = new SimpleBlock(shader, selectorTexture, Vector3.Zero);
            block.Transform.Scale = new Vector3(1.05f, 1.05f, 1.05f);
            currentTexture = selectorTexture;
            PanelUI.OnSlotChanged += OnSelectedSlotWasChanged;
            //block.ChangeUV(UVAtlas.GetUVs(3,3));
        }

        public void OnSelectedSlotWasChanged(short slot)
        {

            if(PanelUI.IsHoldingDrill())
            {
              
                if (!block.IsUsingDefaultUV)
                {
                    block.Texture = selectorTexture;
                    block.Transform.Scale = new Vector3(1.05f, 1.05f, 1.05f);
                    block.ResetUV();
                }
                
            }

            else if (PanelUI.IsHoldingBlock())
            {
               
                //if (block.IsUsingDefaultUV)   // to do block was changed
                //{
                    block.Texture = GameBlocks.BlocksTexture;
                block.Transform.Scale = new Vector3(1,1,1);
                UpdateUV();
                // }


            }
           

        }

        private void UpdateUV()
        {
            block.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Left, blockDirection), Face.Left, false);
            block.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Right, blockDirection), Face.Right, false);
            block.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Back, blockDirection), Face.Back, false);
            block.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Front, blockDirection), Face.Front, false);
            block.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Top, blockDirection), Face.Top, false);
            block.ChangeUV(PanelUI.GetSelectedBlockUV(Face.Bottom, blockDirection), Face.Bottom, false);


            block.RegenerateMesh();
        }

        public void UpdatePosition(Vector3 position, Direction direction)
        {
            if(block.Transform.Position == position) return;

            position += Vector3.One * 0.5f;


            if(!block.IsUsingDefaultUV)
            {
                if (blockDirection != direction)
                {
                    blockDirection = direction;

                    UpdateUV();
                }
            }
            


            block.Transform.Position = position;
        }


        public void Draw(Camera camera)
        {
            if(!IsVisible) return;
            if(!Settings.ShowInterface) return;


            GL.Enable(EnableCap.DepthTest);
            //blockModel.Draw(camera);
            block.Render(camera);

            GL.Disable(EnableCap.DepthTest);


        }
    }
}
