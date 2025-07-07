using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Utils;

namespace Engine
{
    public class Skybox : Node3D
    {
        public Mesh Mesh { get; private set; }
        public MaterialBase Material { get; private set; }

        public bool IsAmbientAffected = false;


        public Skybox(Texture2D texture)
        {
            Init(GenMesh.CreateCube(), new SkyboxMaterial(texture));
        }
        public Skybox(Mesh mesh, Texture2D texture)
        {
            Init(mesh, new SkyboxMaterial(texture));
        }
        public Skybox(Mesh mesh, MaterialBase material)
        {
            Init(mesh, material);
        }

        private void Init(Mesh mesh, MaterialBase material)
        {
            Mesh = mesh;
            Material = material;

            Scale = new Vector3(100, 100, 100);
            Name = "Skybox";
        }

        public override void Render()
        {
            var cam = Camera.Main;
            if (cam == null) return;
            
            Position = cam.Position;

            Material.Apply(GetRenderModelMatrix());

            if (IsAmbientAffected)
                Material.Shader.SetVector3("ambient", Lighting.AmbientColor);
            else
                Material.Shader.SetVector3("ambient", new Vector3(1, 1, 1));  // can be optimized
            
            Mesh.Render();
            
            base.Render();
           
        }

      
    }
}
