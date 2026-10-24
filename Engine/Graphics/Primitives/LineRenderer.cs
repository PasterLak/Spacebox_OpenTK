using OpenTK.Mathematics;
using Engine.Graphics.Rhi;

namespace Engine
{
    public class LineRenderer : Node3D, IDisposable
    {
        public MaterialBase Material;
        private MeshBuffer _buffer;
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

        private Vector3 _upReference = Vector3.UnitY;
        public Vector3 UpReference
        {
            get => _upReference;
            set
            {
                _upReference = value.LengthSquared > 1e-6f ? value.Normalized() : Vector3.UnitY;
                _geometryNeedsUpdate = true;
            }
        }

        public static Vector3 ChooseUp(Vector3 direction, Vector3 upReference)
        {
            if (Math.Abs(Vector3.Dot(direction, upReference)) <= 0.99f) return upReference;
            return Math.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
        }

        private float[] _vertices;
        private uint[] _indices;
        private bool _geometryNeedsUpdate = true;

        public void Pop()
        {
            if(Points.Count > 0)
            {
                Points.RemoveAt(Points.Count-1);
                _geometryNeedsUpdate = true;
            }
        }
        public LineRenderer()
        {
           
            Material = new ColorMaterial();

            var attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "aPosition", Size = 3 },
                new BufferAttribute { Name = "aNormal",   Size = 3 }
            };

            _buffer = new MeshBuffer(attrs);
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
                Vector3 upCandidate = ChooseUp(dir, _upReference);
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

                inds.Add(vertexOffset + 2); inds.Add(vertexOffset + 1); inds.Add(vertexOffset + 0);
                inds.Add(vertexOffset + 0); inds.Add(vertexOffset + 3); inds.Add(vertexOffset + 2);

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

        public override void Render()
        {
            if (!Enabled) return;
            base.Render();
            if (_geometryNeedsUpdate) RebuildGeometry();
            if (_vertices.Length == 0 || _indices.Length == 0) return;
            if (Camera.Main == null) return;

            var camera = Camera.Main;

            var finalModel = GetRenderModelMatrix();

            Material.Color = _color;
            Material.Apply(finalModel);

            _buffer.DrawIndexed(PrimitiveTopology.Triangles, _indices.Length);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }

        public override void Destroy()
        {
            base.Destroy();
            Dispose();
        }
    }
}