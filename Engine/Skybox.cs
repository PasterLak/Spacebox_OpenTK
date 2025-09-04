using Engine.Utils;
using OpenTK.Mathematics;

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


            /*
            GLState.DepthTest(false);
            GLState.DepthMask(false);
            GLState.Blend(false); */

            Material.Apply(GetRenderModelMatrix());


            Mesh.Render();
            //GLState.DepthTest(true);
            //GLState.DepthMask(true);

            base.Render();
           
        }

        public override void Destroy()
        {
            base.Destroy();

            Mesh.Dispose();

        }


    }
}
