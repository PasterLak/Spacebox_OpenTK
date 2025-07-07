
namespace Engine.Components
{

    
    public class MeshRendererComponent : Component
    {
        public Mesh Mesh { get; }
        public MaterialBase Material { get; }

        public MeshRendererComponent(Mesh mesh)
            : this(mesh, new Material()) { }

        public MeshRendererComponent(Mesh mesh, MaterialBase material)
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
            Engine.Debug.Log("----------------------");
            Material.Shader.SetMatrix4("model", Owner.GetRenderModelMatrix());
            Material.Shader.SetMatrix4("view", Camera.Main.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", Camera.Main.GetProjectionMatrix());

            Mesh.Render();

        }

    }
}
