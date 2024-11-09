
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
                block.ChangeUV(PanelUI.GetSelectedBlockUV());
                // }

               
            }
           

        }

        public void UpdatePosition(Vector3 position)
        {
            if(block.Transform.Position == position) return;

 

            position += Vector3.One * 0.5f;

           
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
