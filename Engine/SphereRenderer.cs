using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace Engine
{
    public class SphereRenderer : Node3D, IDisposable
    {
        public bool Enabled = true;
        public MaterialBase Material;
        private MeshBuffer _buffer;

        private float _radius;
        private int _segments;
        private int _rings;
        private Color4 _color = Color4.White;
        public Color4 Color
        {
            get => _color;
            set { _color = value; }
        }

        private float[] _vertices = System.Array.Empty<float>();
        private uint[] _indices = System.Array.Empty<uint>();
        private bool _geometryNeedsUpdate = true;

        public SphereRenderer(Vector3 center, float radius, int segments, int rings)
        {
           // _center = center;
            _radius = radius;
            _segments = segments;
            _rings = rings;
            Position = center;
            Material = new ColorMaterial();
            var attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "aPos",       Size = 3 },
                new BufferAttribute { Name = "aNormal",    Size = 3 },
                new BufferAttribute { Name = "aTexCoords", Size = 2 }
            };
            _buffer = new MeshBuffer(attrs);
        }

        private void RebuildSphere()
        {
            _geometryNeedsUpdate = false;
            var vertList = new List<float>();
            var indList = new List<uint>();
            for (int ring = 0; ring <= _rings; ring++)
            {
                float phi = MathF.PI * ring / _rings;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                for (int seg = 0; seg <= _segments; seg++)
                {
                    float theta = 2f * MathF.PI * seg / _segments;
                    float cosTheta = MathF.Cos(theta);
                    float sinTheta = MathF.Sin(theta);
                    float x = sinPhi * cosTheta;
                    float y = cosPhi;
                    float z = sinPhi * sinTheta;
                    Vector3 normal = new Vector3(x, y, z);
                    Vector3 pos =  normal * _radius;
                    float u = 1f - (float)seg / _segments;
                    float v = 1f - (float)ring / _rings;
                    vertList.Add(pos.X); vertList.Add(pos.Y); vertList.Add(pos.Z);
                    vertList.Add(normal.X); vertList.Add(normal.Y); vertList.Add(normal.Z);
                    vertList.Add(u); vertList.Add(v);
                }
            }
            for (int ring = 0; ring < _rings; ring++)
            {
                for (int seg = 0; seg < _segments; seg++)
                {
                    uint current = (uint)(ring * (_segments + 1) + seg);
                    uint next = (uint)(current + _segments + 1);
                    indList.Add(current);
                    indList.Add(next);
                    indList.Add(current + 1);
                    indList.Add(current + 1);
                    indList.Add(next);
                    indList.Add(next + 1);
                }
            }
            _vertices = vertList.ToArray();
            _indices = indList.ToArray();
            _buffer.BindBuffer(ref _vertices, ref _indices);
            _buffer.SetAttributes();
        }

        public void Render()
        {
            if (!Enabled) return;
            if (_geometryNeedsUpdate) RebuildSphere();
            if (_vertices.Length == 0 || _indices.Length == 0) return;
            if (Camera.Main == null) return;
            var cam = Camera.Main;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);

           
            var matModel = GetRenderModelMatrix();

            Material.Color = _color;
            Material.SetUniforms(matModel);
            Material.Use();

            GL.BindVertexArray(_buffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.CullFace(CullFaceMode.Back);
            GL.Disable(EnableCap.CullFace);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }
    }
}
