
using Engine.Graphics;
using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine
{

    public class ParticleRenderer : IDisposable
    {
        private readonly MeshBuffer _quad;
        private readonly BufferHandle _instVbo;
        private float[] _data;
        private int _count;
        public ParticleMaterial Material;
        private int _maxParticles;
        private bool _flipX;
        private bool _flipY;

        public ParticleRenderer(ParticleMaterial material, int maxParticles)
        {
            Material = material;
            _quad = new MeshBuffer(new[] { new BufferAttribute("aPos", 3), new BufferAttribute("aUV", 2) });
            if (Application.IsHeadless)
            {
                _data = Array.Empty<float>();
                return;
            }

            UpdateQuadUV();
            _instVbo = Gpu.Device.CreateBuffer();
            Gpu.Device.BindVertexArray(_quad.VertexArray);
            Rebuild(maxParticles);
            Gpu.Device.BindVertexArray(VertexArrayHandle.None);
        }

        private void UpdateQuadUV()
        {
            float u0 = _flipX ? 1f : 0f;
            float u1 = _flipX ? 0f : 1f;
            float v0 = _flipY ? 1f : 0f;
            float v1 = _flipY ? 0f : 1f;

            float[] v = {
                -0.5f, -0.5f, 0, u0, v0,
                 0.5f, -0.5f, 0, u1, v0,
                 0.5f,  0.5f, 0, u1, v1,
                -0.5f,  0.5f, 0, u0, v1
            };

            uint[] i = { 0, 1, 2, 2, 3, 0 };
            _quad.BindBuffer(ref v, ref i);
            _quad.SetAttributes();
        }

        public void SetFlip(bool flipX, bool flipY)
        {
            if (_flipX != flipX || _flipY != flipY)
            {
                _flipX = flipX;
                _flipY = flipY;
                UpdateQuadUV();
            }
        }

        public void Rebuild(int maxParticles)
        {
            const int floatsPerInstance = 20;
            _maxParticles = maxParticles;
            _data = new float[_maxParticles * floatsPerInstance];
            _count = 0;
            int stride = floatsPerInstance * sizeof(float);
            if (!_instVbo.IsValid) return;

            var device = Gpu.Device;
            device.AllocateBuffer(BufferKind.Vertex, _instVbo, _maxParticles * stride, BufferUsage.Dynamic);
            int offset = 0, loc = 2;
            for (int c = 0; c < 4; c++)
            {
                device.SetVertexAttribute(loc++, 4, VertexComponentType.Float, false, stride, offset);
                device.SetVertexAttributeDivisor(loc - 1, 1);
                offset += 16;
            }
            device.SetVertexAttribute(loc, 4, VertexComponentType.Float, false, stride, offset);
            device.SetVertexAttributeDivisor(loc, 1);
        }



        public void Begin()
        {


            var cam = Camera.Main;
            if (cam == null) return;

            _count = 0;

            Material.Apply(Matrix4.Identity);
            Material.Shader.SetMatrix4("view", cam.GetViewMatrix(), false);
            Material.Shader.SetMatrix4("projection", cam.GetProjectionMatrix(), false);
            GLState.DepthMask(false);

        }

        public static Vector3 ToRenderPosition(Particle p, Matrix4 systemModel, SimulationSpace space)
        {
            return space == SimulationSpace.Local
                ? Vector3.TransformPosition(p.Position, systemModel)
                : RenderSpace.ToRender(p.Position);
        }

        public void Draw(Particle p, Matrix4 systemModel, SimulationSpace space)
        {
            if (_count >= _maxParticles) return;

            Vector3 worldPos = ToRenderPosition(p, systemModel, space);

            var m = Matrix4.CreateScale(p.Size) * Matrix4.CreateTranslation(worldPos);
            var c = p.Color;
            int b = _count * 20;
            _data[b + 0] = m.M11; _data[b + 1] = m.M12; _data[b + 2] = m.M13; _data[b + 3] = m.M14;
            _data[b + 4] = m.M21; _data[b + 5] = m.M22; _data[b + 6] = m.M23; _data[b + 7] = m.M24;
            _data[b + 8] = m.M31; _data[b + 9] = m.M32; _data[b + 10] = m.M33; _data[b + 11] = m.M34;
            _data[b + 12] = m.M41; _data[b + 13] = m.M42; _data[b + 14] = m.M43; _data[b + 15] = m.M44;
            _data[b + 16] = c.X; _data[b + 17] = c.Y; _data[b + 18] = c.Z; _data[b + 19] = c.W;
            _count++;
        }

        public void End()
        {
            if (!_instVbo.IsValid) return;

            var device = Gpu.Device;
            device.BufferSubData(BufferKind.Vertex, _instVbo, 0, new ReadOnlySpan<float>(_data, 0, _count * 20));
            device.BindVertexArray(_quad.VertexArray);
            device.DrawIndexedInstanced(PrimitiveTopology.Triangles, 6, IndexType.UnsignedInt, 0, _count);
            if (Material.ClearsAmbientOcclusion) DrawAmbientOcclusionMask();
            device.BindVertexArray(VertexArrayHandle.None);

            GLState.DepthMask(true);
            GLState.DepthTest(true);
            
        }

        private void DrawAmbientOcclusionMask()
        {
            var device = Gpu.Device;
            device.SetColorWrite(0, false);
            GLState.GBufferWrite(true);
            GLState.Blend(false);
            Material.Shader.SetBool("aoMaskPass", true);
            device.DrawIndexedInstanced(PrimitiveTopology.Triangles, 6, IndexType.UnsignedInt, 0, _count);
            Material.Shader.SetBool("aoMaskPass", false);
            device.SetColorWrite(0, true);
            GLState.GBufferWrite(false);
            GLState.Blend(true);
        }

        public void Dispose()
        {
            _quad.Dispose();
            Gpu.Device.DeleteBuffer(_instVbo);
            
        }
    }
}
