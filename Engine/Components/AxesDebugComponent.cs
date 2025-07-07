

namespace Engine.Components.Debug
{
    public sealed class AxesDebugComponent : Component
    {
      
        private readonly float _length;
 
        public AxesDebugComponent(float length = 1f)
        {
            _length = length;
        }

        public override void OnAttached(Node3D owner)
        {
            base.OnAttached(owner); 
        }

        public override void OnRender()
        {

            VisualDebug.DrawAxes(Owner, _length);
           
        }

        public override void OnDetached()
        {
           
        }

      
    }
}
