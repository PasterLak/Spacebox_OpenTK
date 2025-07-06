

namespace Engine.Components.Debug
{
    public sealed class AxesDebugComponent : Component
    {
      
        private readonly float _length;
 
        public AxesDebugComponent(float length = 1f)
        {
            _length = length;
        }

        public override void OnAttached()
        {
        
        }

        public override void Render()
        {

            VisualDebug.DrawAxes(Owner, _length);
           
        }

        public override void OnDettached()
        {
           
        }

      
    }
}
