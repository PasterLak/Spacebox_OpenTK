
using Engine;
using OpenTK.Mathematics;


namespace Spacebox.FPS
{
    public class Water : Model, ITransparent
    {
        public Water(Shader shader) :
            base(Resources.Load<Mesh>("Resources/Models/plane.obj"), new Material(shader,
                Resources.Get<Texture2D>("Resources/Textures/Game/water.png")))
        {

           // Material.Tiling = new Vector2(40, 40);
            Material.Color = new Color4(1, 1, 1, 0.5f);

        }

        public void DrawTransparent(Camera camera)
        {
            Update();

            Render(camera);
        }

        public void Update()
        {
            //Material.Offset -= new Vector2(0.15f * Time.Delta, 0);
            Material.Shader.SetVector3("ambientColor", Lighting.AmbientColor);


        }
    }
}
