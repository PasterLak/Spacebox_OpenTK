

using Engine.Graphics.Transparency;
using OpenTK.Mathematics;

namespace Engine.Components
{
    public class ModelRendererComponent : Component, Engine.Light.IExtraLightTarget, ITransparentRenderable
    {
        public Vector3 Offset { get; set; } = new Vector3(0);
        public Model Model { get; set; }
        public Vector3 ExtraLight { get; set; }
     
        public ModelRendererComponent(Model model)
        {
            Model = model;
        }

        public override void OnAttached(Node3D owner)
        {
            base.OnAttached(owner); 
        }

        public override void OnRender()
        {
            if (Model == null || Model.Material == null || Camera.Main == null) return;

            if (Model.Material.IsTransparent &&
                TransparentQueue.TrySubmit(this, Owner.GetRenderModelMatrix().ExtractTranslation(), Model.Material.RenderQueue))
                return;

            Draw();
        }

        public void RenderTransparent()
        {
            if (Model == null || Model.Material == null || Camera.Main == null) return;
            Draw();
        }

        private void Draw()
        {
            var matrix = Owner.GetRenderModelMatrix();
            if (Offset != Vector3.Zero)
            {
                matrix *= Matrix4.CreateTranslation(Offset);
            }
            if (Model.Material is TextureMaterial textureMaterial) textureMaterial.ExtraLight = ExtraLight;
            Model.Material.Apply(matrix);
         
            //Material.Shader.SetMatrix4("model", GetModelMatrix());
            Model.Material.Shader.SetMatrix4("view", Camera.Main.GetViewMatrix());
            Model.Material.Shader.SetMatrix4("projection", Camera.Main.GetProjectionMatrix());
            Model.Mesh.Render();

        }

        public override void OnDetached()
        {
            base.OnDetached();
            Model?.Destroy();
        }

    }
}
