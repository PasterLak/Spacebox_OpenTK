
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public sealed class ParticleRenderer : IDisposable // new rendererer
    {
        private readonly ParticleSystem _ps;
        public ParticleMaterial Material { get; set; }
        private readonly MeshBuffer _quad;
        private readonly int _instanceVbo;
        private readonly float[] _instanceData;
        private int _aliveCount;
        public bool RandomRotation { get; set; }
        private byte _rotationCase;
        private static readonly Random _rng = new Random();

        public ParticleRenderer(ParticleSystem ps, ParticleMaterial mat)
        {
            _ps = ps;
            Material = mat;
            RandomRotation = ps.Emitter.RandomUVRotation;

            _quad = new MeshBuffer(new[]
            {
                new BufferAttribute { Name = "aPos", Size = 3 },
                new BufferAttribute { Name = "aUV",  Size = 2 }
            });

            float[] verts =
            {
                -0.5f, -0.5f, 0f, 0f, 0f,
                 0.5f, -0.5f, 0f, 1f, 0f,
                 0.5f,  0.5f, 0f, 1f, 1f,
                -0.5f,  0.5f, 0f, 0f, 1f
            };
            uint[] idx = { 0, 1, 2, 2, 3, 0 };
            _quad.BindBuffer(ref verts, ref idx);
            _quad.SetAttributes();

            _instanceVbo = GL.GenBuffer();
            GL.BindVertexArray(_quad.VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, ps.MaxParticles * 20 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            int stride = 20 * sizeof(float);
            int offset = 0;
            for (int col = 0; col < 4; col++)
            {
                GL.EnableVertexAttribArray(2 + col);
                GL.VertexAttribPointer(2 + col, 4, VertexAttribPointerType.Float, false, stride, offset);
                GL.VertexAttribDivisor(2 + col, 1);
                offset += 4 * sizeof(float);
            }

            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, stride, 16 * sizeof(float));
            GL.VertexAttribDivisor(6, 1);

         
            GL.BindVertexArray(0);

            _instanceData = new float[ps.MaxParticles * 20];
            RandomizeRotation();
        }

        public void SetFixedRotation180() => _rotationCase = 3;

        public void SetRandomRotation(bool enable)
        {
            _rotationCase = 0;
            if (enable) RandomizeRotation();
        }

        private void RandomizeRotation()
        {
            if (RandomRotation) _rotationCase = (byte)_rng.Next(0, 4);
        }

        public void Update()
        {
            RandomizeRotation();
            _aliveCount = 0;

            foreach (var p in _ps.GetParticles())
            {
                Vector3 pos = _ps.UseLocalCoordinates ? p.Position + _ps.Position : p.Position;

                if (Camera.Main != null && Camera.Main.CameraRelativeRender) pos -= Camera.Main.Position; // ?????

                Matrix4 m = Matrix4.CreateScale(p.Size) * Matrix4.CreateTranslation(pos);
                Vector4 c = p.GetCurrentColor();

                int baseI = _aliveCount * 20;
                _instanceData[baseI + 0] = m.M11; _instanceData[baseI + 1] = m.M12; _instanceData[baseI + 2] = m.M13; _instanceData[baseI + 3] = m.M14;
                _instanceData[baseI + 4] = m.M21; _instanceData[baseI + 5] = m.M22; _instanceData[baseI + 6] = m.M23; _instanceData[baseI + 7] = m.M24;
                _instanceData[baseI + 8] = m.M31; _instanceData[baseI + 9] = m.M32; _instanceData[baseI + 10] = m.M33; _instanceData[baseI + 11] = m.M34;
                _instanceData[baseI + 12] = m.M41; _instanceData[baseI + 13] = m.M42; _instanceData[baseI + 14] = m.M43; _instanceData[baseI + 15] = m.M44;
                _instanceData[baseI + 16] = c.X; _instanceData[baseI + 17] = c.Y; _instanceData[baseI + 18] = c.Z; _instanceData[baseI + 19] = c.W;

                _aliveCount++;
                if (_aliveCount >= _ps.MaxParticles) break;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _aliveCount * 20 * sizeof(float), _instanceData);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
       
        public void Render(Camera cam)
        {
            if (_aliveCount == 0) return;
           


            Material.Apply(Matrix4.Identity);
            Material.Shader.SetMatrix4("view", cam.GetViewMatrix(), false);
            Material.Shader.SetMatrix4("projection", cam.GetProjectionMatrix(), false);
            Material.Shader.SetInt("rotationCase", _rotationCase);
            

            GL.BindVertexArray(_quad.VAO);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero, _aliveCount);
            GL.BindVertexArray(0);

        }

        public void Dispose()
        {
            _quad.Dispose();
            GL.DeleteBuffer(_instanceVbo);
        }
    }
}
