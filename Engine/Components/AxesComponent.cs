using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Components;
using Engine.Utils;

namespace Engine.Components.Debug
{
    public sealed class AxesComponent : Component
    {
        private static  uint[] Indices = { 0u, 1u, 2u, 3u, 4u, 5u };
        private readonly float _length;
        private float[] _vertices;
        private Shader _shader;
        private MeshBuffer _buffer;

        public AxesComponent(float length = 1f)
        {
            _length = length;
        }

        public override void OnAttached()
        {
            _shader = Resources.Load<Shader>("Shaders/axes", true);
            _buffer = new MeshBuffer(new[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color",    Size = 3 }
            });
            BuildVertices();
            _buffer.BindBuffer(ref _vertices, ref Indices);
            _buffer.SetAttributes();
        }

        public override void Render()
        {
            if (Camera.Main == null) return;

            VisualDebug.DrawAxes(Owner, 1f);
            _shader.Use();
            _shader.SetMatrix4("model", Owner.GetRenderModelMatrix());
            _shader.SetMatrix4("view", Camera.Main.GetViewMatrix());
            _shader.SetMatrix4("projection", Camera.Main.GetProjectionMatrix());
            bool depthEnabled = GL.IsEnabled(EnableCap.DepthTest);
            if (depthEnabled) GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.LineWidth(2f);
            GL.BindVertexArray(_buffer.VAO);
            GL.DrawElements(PrimitiveType.Lines, Indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            if (depthEnabled) GL.Enable(EnableCap.DepthTest);
        }

        public override void OnDettached()
        {
            _buffer?.Dispose();
        }

        private void BuildVertices()
        {
            float l = _length;
            _vertices = new[]
            {
                0f, 0f, 0f, 1f, 0f, 0f,
                l,  0f, 0f, 1f, 0f, 0f,
                0f, 0f, 0f, 0f, 1f, 0f,
                0f, l,  0f, 0f, 1f, 0f,
                0f, 0f, 0f, 0f, 0f, 1f,
                0f, 0f, l,  0f, 0f, 1f
            };
        }
    }
}
