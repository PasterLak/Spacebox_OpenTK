using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Spacebox.Common
{
    public class ParticleRenderer : IDisposable
    {
        private Shader shader;
        private Texture2D texture;
        private ParticleSystem particleSystem;

        private int vao;
        private int vbo;

        private List<Matrix4> instanceTransforms = new List<Matrix4>();
        private List<Vector4> instanceColors = new List<Vector4>();
        private List<float> instanceSizes = new List<float>();

        public ParticleRenderer(Shader shader, Texture2D texture, ParticleSystem system)
        {
            this.shader = shader;
            this.texture = texture;
            this.particleSystem = system;

            Initialize();
        }

        private void Initialize()
        {
            // Вершины квадрата (билборда)
            float[] vertices = {
                // Positions     // Texture Coords
                -0.5f, -0.5f, 0f, 0f, 0f,
                 0.5f, -0.5f, 0f, 1f, 0f,
                 0.5f,  0.5f, 0f, 1f, 1f,
                -0.5f,  0.5f, 0f, 0f, 1f
            };

            uint[] indices = {
                0, 1, 2,
                2, 3, 0
            };

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Позиции вершин
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            // Координаты текстуры
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            // Буферы для трансформаций частиц
            int instanceVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, 1000 * (16 + 4 + sizeof(float)) * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            // Трансформации
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (16 + 4 + sizeof(float)) * sizeof(float), 0);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, (16 + 4 + sizeof(float)) * sizeof(float), 4 * sizeof(float));
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, (16 + 4 + sizeof(float)) * sizeof(float), 8 * sizeof(float));
            GL.EnableVertexAttribArray(5);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, (16 + 4 + sizeof(float)) * sizeof(float), 12 * sizeof(float));

            // Цвета
            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (16 + 4 + sizeof(float)) * sizeof(float), 16 * sizeof(float));

            // Размеры
            GL.EnableVertexAttribArray(7);
            GL.VertexAttribPointer(7, 1, VertexAttribPointerType.Float, false, (16 + 4 + sizeof(float)) * sizeof(float), 20 * sizeof(float));

            // Настройка атрибутов для экземпляров
            GL.VertexAttribDivisor(2, 1);
            GL.VertexAttribDivisor(3, 1);
            GL.VertexAttribDivisor(4, 1);
            GL.VertexAttribDivisor(5, 1);
            GL.VertexAttribDivisor(6, 1);
            GL.VertexAttribDivisor(7, 1);

            GL.BindVertexArray(0);
        }

        public void UpdateBuffers()
        {
            instanceTransforms.Clear();
            instanceColors.Clear();
            instanceSizes.Clear();

            foreach (var particle in particleSystem.GetParticles())
            {
                // Матрица трансформации для билборда
                Matrix4 model = Matrix4.CreateScale(particle.Size) *
                                Matrix4.CreateTranslation(particle.Position);

                instanceTransforms.Add(model);
                instanceColors.Add(particle.Color * particle.GetAlpha());
                instanceSizes.Add(particle.Size);
            }

            // Обновляем буфер
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Связываем VAO
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Используем инстансный буфер
            int instanceVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO);
            int dataSize = instanceTransforms.Count * (16 + 4 + sizeof(float)) * sizeof(float);
            GL.BufferData(BufferTarget.ArrayBuffer, dataSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, dataSize, PrepareInstanceData());

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        private float[] PrepareInstanceData()
        {
            List<float> data = new List<float>();
            for (int i = 0; i < instanceTransforms.Count; i++)
            {
                Matrix4 mat = instanceTransforms[i];
                Vector4 color = instanceColors[i];
                float size = instanceSizes[i];

                // Матрица модели (16 элементов)
                data.AddRange(new float[] {
                    mat.M11, mat.M12, mat.M13, mat.M14,
                    mat.M21, mat.M22, mat.M23, mat.M24,
                    mat.M31, mat.M32, mat.M33, mat.M34,
                    mat.M41, mat.M42, mat.M43, mat.M44
                });

                // Цвет (4 элемента)
                data.AddRange(new float[] { color.X, color.Y, color.Z, color.W });

                // Размер (1 элемент)
                data.Add(size);
            }

            return data.ToArray();
        }

        public void Render(Camera camera)
        {
            shader.Use();

            // Передаём матрицу вида и проекции
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());

            // Связываем текстуру
            texture.Use(TextureUnit.Texture0);
            shader.SetInt("particleTexture", 0);

            // Включаем альфа-тестинг для прозрачности
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindVertexArray(vao);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero, particleSystem.GetParticles().Count);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.Blend);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            // Удаляем другие буферы, если они существуют
        }
    }
}
