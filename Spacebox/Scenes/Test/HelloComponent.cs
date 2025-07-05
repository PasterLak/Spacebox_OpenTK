
using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Scenes.Test
{
    public class HelloComponent : Component
    {
        public override void OnAttached()
        {
            Debug.Log("Attached!");
        }

        public override void Update()
        {
            
        }
    }
}
