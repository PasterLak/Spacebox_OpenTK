using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace Spacebox.Common
{
    public class LineRenderer : Node3D, IDisposable
    {
        public bool Enabled = true;
        private Shader _shader;
        private BufferShader _buffer;
        public List<Vector3> Points = new List<Vector3>();
        private float _thickness = 0.1f;
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                _geometryNeedsUpdate = true;
            }
        }
        private Color4 _color = Color4.Red;
        public Color4 Color
        {
            get => _color;
            set
            {
                _color = value;
                _geometryNeedsUpdate = true;
            }
        }

        private float[] _vertices;
        private uint[] _indices;
        private bool _geometryNeedsUpdate = true;

        public LineRenderer()
        {
            _shader = ShaderManager.GetShader("Shaders/lineRenderer");

            var attrs = new BufferShader.Attribute[]
            {
                new BufferShader.Attribute { Name = "aPosition", Size = 3 },
                new BufferShader.Attribute { Name = "aNormal",   Size = 3 }
            };

            _buffer = new BufferShader(attrs);
        }

        public void SetPoints(List<Vector3> points)
        {
            Points = points;
            _geometryNeedsUpdate = true;
        }

        public void AddPoint(Vector3 point)
        {
            Points.Add(point);
            _geometryNeedsUpdate = true;
        }

        public void ClearPoints()
        {
            Points.Clear();
            _geometryNeedsUpdate = true;
        }
        public void SetNeedsRebuild()
        {
            _geometryNeedsUpdate = true;
        }
        private void RebuildGeometry()
        {
            _geometryNeedsUpdate = false;
            if (Points.Count < 2)
            {
                _vertices = Array.Empty<float>();
                _indices = Array.Empty<uint>();
                return;
            }

            var verts = new List<float>();
            var inds = new List<uint>();
            uint vertexOffset = 0;

            for (int i = 0; i < Points.Count - 1; i++)
            {
                Vector3 p0 = Points[i];
                Vector3 p1 = Points[i + 1];
                Vector3 segment = (p1 - p0);
                float length = segment.Length;
                if (length < 1e-6f) continue;

                Vector3 dir = segment.Normalized();
                Vector3 upCandidate = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
                Vector3 side1 = Vector3.Cross(dir, upCandidate).Normalized();
                Vector3 side2 = Vector3.Cross(dir, side1).Normalized();
                float r = _thickness * 0.5f;

                Vector3 p0a = p0 + side1 * r + side2 * r;
                Vector3 p0b = p0 - side1 * r + side2 * r;
                Vector3 p0c = p0 - side1 * r - side2 * r;
                Vector3 p0d = p0 + side1 * r - side2 * r;
                Vector3 p1a = p1 + side1 * r + side2 * r;
                Vector3 p1b = p1 - side1 * r + side2 * r;
                Vector3 p1c = p1 - side1 * r - side2 * r;
                Vector3 p1d = p1 + side1 * r - side2 * r;

                Vector3 n0 = Vector3.Zero;

                var vList = new Vector3[] { p0a, p0b, p0c, p0d, p1a, p1b, p1c, p1d };
                for (int v = 0; v < vList.Length; v++)
                {
                    verts.Add(vList[v].X);
                    verts.Add(vList[v].Y);
                    verts.Add(vList[v].Z);
                    verts.Add(n0.X);
                    verts.Add(n0.Y);
                    verts.Add(n0.Z);
                }

                inds.Add(vertexOffset + 0); inds.Add(vertexOffset + 1); inds.Add(vertexOffset + 2);
                inds.Add(vertexOffset + 2); inds.Add(vertexOffset + 3); inds.Add(vertexOffset + 0);
                inds.Add(vertexOffset + 4); inds.Add(vertexOffset + 5); inds.Add(vertexOffset + 6);
                inds.Add(vertexOffset + 6); inds.Add(vertexOffset + 7); inds.Add(vertexOffset + 4);

                inds.Add(vertexOffset + 0); inds.Add(vertexOffset + 1); inds.Add(vertexOffset + 5);
                inds.Add(vertexOffset + 5); inds.Add(vertexOffset + 4); inds.Add(vertexOffset + 0);

                inds.Add(vertexOffset + 1); inds.Add(vertexOffset + 2); inds.Add(vertexOffset + 6);
                inds.Add(vertexOffset + 6); inds.Add(vertexOffset + 5); inds.Add(vertexOffset + 1);

                inds.Add(vertexOffset + 2); inds.Add(vertexOffset + 3); inds.Add(vertexOffset + 7);
                inds.Add(vertexOffset + 7); inds.Add(vertexOffset + 6); inds.Add(vertexOffset + 2);

                inds.Add(vertexOffset + 3); inds.Add(vertexOffset + 0); inds.Add(vertexOffset + 4);
                inds.Add(vertexOffset + 4); inds.Add(vertexOffset + 7); inds.Add(vertexOffset + 3);

                vertexOffset += 8;
            }

            _vertices = verts.ToArray();
            _indices = inds.ToArray();
            _buffer.BindBuffer(ref _vertices, ref _indices);
            _buffer.SetAttributes();
        }

        public void Render()
        {
            if (!Enabled) return;
            if (_geometryNeedsUpdate) RebuildGeometry();
            if (_vertices.Length == 0 || _indices.Length == 0) return;
            if (Camera.Main == null) return;

            var camera = Camera.Main;

            _shader.Use();

            var finalModel = GetModelMatrix();

            _shader.SetMatrix4("uModel", finalModel, false);
            _shader.SetMatrix4("uView", camera.GetViewMatrix(), false);
            _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix(), false);
            _shader.SetVector4("uColor", (Vector4)_color);

            GL.BindVertexArray(_buffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }
    }
}