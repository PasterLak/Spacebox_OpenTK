using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace Spacebox.Common
{
    public class ParticleRenderer : IDisposable
    {
        public readonly Shader shader;
        private Texture2D texture;
        private ParticleSystem particleSystem;

        private int vao;
        private int vbo;
        private int ebo;
        private int instanceVBO;

        private List<Matrix4> instanceTransforms = new List<Matrix4>();
        private List<Vector4> instanceColors = new List<Vector4>();

        private bool _randomRotation;
        private byte rotationCase = 0;

        public ParticleRenderer(Texture2D texture, ParticleSystem system, Shader shader)
        {
            this.shader = shader;
            this.texture = texture;
            this.particleSystem = system;

            _randomRotation = system.Emitter.RandomUVRotation;
            Initialize();
        }

   

        private void Initialize()
        {
            float[] vertices = {
                -0.5f, -0.5f, 0f, 0f, 0f,
                 0.5f, -0.5f, 0f, 1f, 0f,
                 0.5f,  0.5f, 0f, 1f, 1f,
                -0.5f,  0.5f, 0f, 0f, 1f
            };

            if(_randomRotation)
            {
                Random random = new Random();
                rotationCase = (byte)random.Next(0, 4);
            }

            uint[] indices = {
                0, 1, 2,
                2, 3, 0
            };

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            instanceVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, particleSystem.MaxParticles * (16 + 4) * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            for (int i = 0; i < 4; i++)
            {
                GL.EnableVertexAttribArray(2 + i);
                GL.VertexAttribPointer(2 + i, 4, VertexAttribPointerType.Float, false, (16 + 4) * sizeof(float), i * 4 * sizeof(float));
                GL.VertexAttribDivisor(2 + i, 1);
            }

            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (16 + 4) * sizeof(float), 16 * sizeof(float));
            GL.VertexAttribDivisor(6, 1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void UpdateBuffers()
        {
            instanceTransforms.Clear();
            instanceColors.Clear();

            foreach (var particle in particleSystem.GetParticles())
            {
                Vector3 finalPosition = particleSystem.UseLocalCoordinates
                    ? particle.Position + particleSystem.Position
                    : particle.Position;
                Matrix4 model = Matrix4.CreateScale(particle.Size) * Matrix4.CreateTranslation(finalPosition);
                instanceTransforms.Add(model);
                instanceColors.Add(particle.GetCurrentColor());
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO);

            float[] data = new float[instanceTransforms.Count * (16 + 4)];
            for (int i = 0; i < instanceTransforms.Count; i++)
            {
                Matrix4 mat = instanceTransforms[i];
                Vector4 color = instanceColors[i];

                data[i * 20 + 0] = mat.M11;
                data[i * 20 + 1] = mat.M12;
                data[i * 20 + 2] = mat.M13;
                data[i * 20 + 3] = mat.M14;

                data[i * 20 + 4] = mat.M21;
                data[i * 20 + 5] = mat.M22;
                data[i * 20 + 6] = mat.M23;
                data[i * 20 + 7] = mat.M24;

                data[i * 20 + 8] = mat.M31;
                data[i * 20 + 9] = mat.M32;
                data[i * 20 + 10] = mat.M33;
                data[i * 20 + 11] = mat.M34;

                data[i * 20 + 12] = mat.M41;
                data[i * 20 + 13] = mat.M42;
                data[i * 20 + 14] = mat.M43;
                data[i * 20 + 15] = mat.M44;

                data[i * 20 + 16] = color.X;
                data[i * 20 + 17] = color.Y;
                data[i * 20 + 18] = color.Z;
                data[i * 20 + 19] = color.W;
            }

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Length * sizeof(float), data);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        
        
        public void Render(Camera camera)
        {
            shader.Use();

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();

            shader.SetMatrix4("view", view, false);
            shader.SetMatrix4("projection", projection, false);

            if(_randomRotation)
            shader.SetInt("rotationCase", rotationCase );

            texture.Use(TextureUnit.Texture0);
            shader.SetInt("particleTexture", 0);
            

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(false);

            GL.BindVertexArray(vao);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero, instanceTransforms.Count);
            GL.BindVertexArray(0);

            GL.DepthMask(true);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            if (FramebufferCapture.IsActive)
                FramebufferCapture.SaveFrame();
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GL.DeleteBuffer(instanceVBO);
        }
    }
}
