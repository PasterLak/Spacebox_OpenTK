using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Utils;

namespace Engine
{
    public class Skybox : Node3D, ITransparent
    {
        public Mesh Mesh { get; private set; }
        public MaterialBase Material { get; private set; }

        public bool IsAmbientAffected = false;

        public Skybox(string objPath, Texture2D texture)
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);
            Material = new SkyboxMaterial(texture);
           

            Scale = new Vector3(100, 100, 100);
            Name = "Skybox";


        }

        public void DrawTransparent(Camera camera)
        {

            Position = camera.Position;

            bool cullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);


            Material.SetUniforms(GetModelMatrix());


            if (IsAmbientAffected)
                Material.Shader.SetVector3("ambient", Lighting.AmbientColor);
            else
                Material.Shader.SetVector3("ambient", new Vector3(1, 1, 1));  // can be optimized

            Material.Use();
            Mesh.Draw();

        }
    }
}
