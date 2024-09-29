using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Renderer
    {
        private List<IDrawable> _drawables;

        public Renderer()
        {
            _drawables = new List<IDrawable>();
        }

        public void AddDrawable(IDrawable drawable)
        {
            _drawables.Add(drawable);
        }

        public void RemoveDrawable(IDrawable drawable)
        {
            _drawables.Remove(drawable);
        }

        public void RenderAll(Camera camera)
        {
            foreach (var drawable in _drawables)
            {
                drawable.Draw(camera);
            }
        }
    }
}
