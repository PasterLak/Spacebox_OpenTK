using Engine;
using Engine.Light;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace Spacebox.Scenes.Test
{

    public class Skybox2 : SceneNode, ITransparent
    {
        public Mesh Mesh { get; private set; }
        public MaterialBase Material { get; private set; }

        public bool IsAmbientAffected = false;

        public Skybox2(Mesh mesh, Texture2D texture)
        {
            Mesh = mesh;
            Material = new SkyboxMaterial(texture);

            Scale = new Vector3(100, 100, 100);
            Name = "Skybox";
        }


        public void DrawTransparent(Camera camera)
        {

            //SetPosition(camera.Position);

         
                SetPosition(camera.Position); 

            bool cullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);

            Material.Apply(WorldMatrix);

            if (IsAmbientAffected)
                Material.Shader.SetVector3("ambient", Lighting.AmbientColor);
            else
                Material.Shader.SetVector3("ambient", new Vector3(1, 1, 1));  // can be optimized

           
            Mesh.Render();

        }


    }
}
