

namespace Engine.Components
{
    public class GizmoIconComponent : BillboardComponent
    {
        public GizmoIconComponent(Texture2D texture, float w = 1, float h = 1) : base(texture, w, h)
        {
        }

        public override void OnUpdate()
        {

                Enabled = VisualDebug.Enabled;
   
            base.OnUpdate();
        }
    }
}
