using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class Renderer
    {
        private List<INotTransparent> _opaqueDrawables = new List<INotTransparent>();
        private List<ITransparent> _transparentDrawables = new List<ITransparent>();

        private List<Node3D> _transforms = new List<Node3D>();
        public List<Node3D> GetObjects()
        {
            return _transforms;
        }

        public void AddDrawable(IDrawable drawable)
        {
            if (drawable is ITransparent transparent)
            {
                _transparentDrawables.Add(transparent);
                _transforms.Add((Node3D)transparent);
            }
            if (drawable is INotTransparent opaque)
            {
                _opaqueDrawables.Add(opaque);
                _transforms.Add((Node3D)opaque);
            }
        }

        public void RemoveDrawable(IDrawable drawable)
        {
            if (drawable is ITransparent transparent)
            {
                _transparentDrawables.Remove(transparent);
                _transforms.Remove((Node3D)transparent);
            }
            else if (drawable is INotTransparent opaque)
            {
                _opaqueDrawables.Remove(opaque);
                _transforms.Remove((Node3D)opaque);
            }
        }

        public void RenderAll(Camera camera)
        {
          
            foreach (var drawable in _opaqueDrawables)
            {
                drawable.Render(camera);
            }
         
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false); 

         
            var sortedTransparent = _transparentDrawables
                .OrderByDescending(d => Vector3.Distance(camera.Position, ((Node3D)d).Position))
                .ToList();

            foreach (var transparent in sortedTransparent)
            {
                transparent.DrawTransparent(camera);
            }

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
        }
    }
}