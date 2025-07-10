using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Utils;
using Engine.Light;

namespace Engine
{
    public class Skybox : Node3D
    {
        public Mesh Mesh { get; private set; }
        public MaterialBase Material { get; private set; }

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

           

            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.Disable(EnableCap.Blend);

            Material.Apply(GetRenderModelMatrix());


            Mesh.Render();
            GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);

            base.Render();
           
        }

        public override void Destroy()
        {
            base.Destroy();

            Mesh.Dispose();

        }


    }
}
