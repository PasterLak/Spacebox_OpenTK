

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

            Model.Position = Owner.Position;
           Model.Render(Camera.Main);

        }

    }
}
