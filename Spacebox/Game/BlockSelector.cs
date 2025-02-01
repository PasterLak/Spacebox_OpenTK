
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using System.Reflection;

namespace Spacebox.Game
{
    public class BlockSelector
    {

        public static BlockSelector Instance;
        public static bool IsVisible = false;


        private Texture2D selectorTexture;

        public SimpleBlock SimpleBlock { get; private set; }
        private Texture2D currentTexture;

        private Direction blockDirection = Direction.Up;
        public BlockSelector() 
        {
            Instance = this;
            Shader shader = ShaderManager.GetShader("Shaders/textured");
            selectorTexture = TextureManager.GetTexture("Resources/Textures/selector.png", true);

          

            SimpleBlock = new SimpleBlock(shader, selectorTexture, Vector3.Zero);
            SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);
            currentTexture = selectorTexture;
            PanelUI.OnSlotChanged += OnSelectedSlotWasChanged;
            //block.ChangeUV(UVAtlas.GetUVs(3,3));

            OnSelectedSlotWasChanged(PanelUI.SelectedSlotId);
        }

        public void OnSelectedSlotWasChanged(short slot)
        {

            if(PanelUI.IsHoldingDrill())
            {
              
                if (!SimpleBlock.IsUsingDefaultUV)
                {
                    SimpleBlock.Texture = selectorTexture;
                    SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);
                    SimpleBlock.ResetUV();
                }
                
            }

            else if (PanelUI.IsHoldingBlock())
            {
               
                //if (block.IsUsingDefaultUV)   // to do block was changed
                //{
                    SimpleBlock.Texture = GameBlocks.BlocksTexture;
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
        }


        public void Draw(Camera camera)
        {
            if(!IsVisible) return;
            if(!Settings.ShowInterface) return;


            GL.Enable(EnableCap.DepthTest);
            //blockModel.Draw(camera);
            SimpleBlock.Render(camera);

            GL.Disable(EnableCap.DepthTest);


        }
    }
}
