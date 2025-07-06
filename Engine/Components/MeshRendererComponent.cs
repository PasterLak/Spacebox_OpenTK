

namespace Engine.Components
{
    public class MeshRendererComponent : Component
    {
        public Mesh Mesh { get; }
        public Material Material { get; }

        public MeshRendererComponent(Mesh mesh)
            : this(mesh, new Material()) { }

        public MeshRendererComponent(Mesh mesh, Material material)
        {
            Mesh = mesh;
            Material = material;
        }

        public override void OnAttached()
        {

        }

        public override void Render()
        {
            if (Mesh == null || Material == null || Camera.Main == null) return;

            Material.Use();
            
            Material.Shader.SetMatrix4("model", Owner.GetModelMatrix());
            Material.Shader.SetMatrix4("view", Camera.Main.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", Camera.Main.GetProjectionMatrix());

            Mesh.Render();

        }

    }
}
