

namespace Engine.Components
{
    public class ModelRendererComponent : Component
    {
        public Model Model { get; }
     
        public ModelRendererComponent(Model model)
        {
            Model = model;
        }

        public override void OnAttached()
        {

        }

        public override void Render()
        {
            if (Model == null || Model.Material == null || Camera.Main == null) return;

            Model.Material.Apply(Owner);
         
            //Material.Shader.SetMatrix4("model", GetModelMatrix());
            Model.Material.Shader.SetMatrix4("view", Camera.Main.GetViewMatrix());
            Model.Material.Shader.SetMatrix4("projection", Camera.Main.GetProjectionMatrix());
            Model.Mesh.Render();

        }

    }
}
