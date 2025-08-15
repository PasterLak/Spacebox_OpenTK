using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.PostProcessing
{
    public class SSAOEffect : PostProcessEffect, IDisposable
    {
        private readonly Shader shader;
        private readonly SceneRenderer renderer;
        private readonly Texture2D noiseTex;
        private readonly Vector3[] samples;
        private readonly int sampleCount;
        private bool disposed;

        public float Radius { get; set; } = 0.1f;
        public float Bias { get; set; } = 0.02f;
        public float Intensity { get; set; } = 1.0f;

        public SSAOEffect(Shader shader, SceneRenderer renderer, Texture2D noise = null, int sampleCount = 32)
        {
            this.shader = shader;
            this.renderer = renderer;
            this.noiseTex = noise;
            this.sampleCount = Math.Clamp(sampleCount, 8, 64);
            samples = BuildKernel(this.sampleCount);

            shader.Use();
            shader.SetInt("uSampleCount", this.sampleCount);
            for (int i = 0; i < this.sampleCount; i++)
                shader.SetVector3($"uSamples[{i}]", samples[i]);
        }

        public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
        {
            if (!Enabled) return;
            var cam = Camera.Main;
            if (cam == null) return;

            //Debug.Log("sssao teture is null " + (noiseTex == null));

            shader.Use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, inputTexture);
            shader.SetInt("uColor", 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, renderer.NormalTexture);
            shader.SetInt("uNormalMap", 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, renderer.DepthTexture);
            shader.SetInt("uDepthMap", 2);

            if (noiseTex != null)
            {
                GL.ActiveTexture(TextureUnit.Texture3);
                GL.BindTexture(TextureTarget.Texture2D, noiseTex.Handle);
                shader.SetInt("uNoiseMap", 3);
                shader.SetVector2("uNoiseScale", new Vector2(clientSize.X / (float)noiseTex.Width, clientSize.Y / (float)noiseTex.Height));
                shader.SetInt("uHasNoise", 1);
            }
            else
            {
                shader.SetVector2("uNoiseScale", Vector2.One);
                shader.SetInt("uHasNoise", 0);
            }

            var proj = cam.GetProjectionMatrix();
            var invProj = proj.Inverted();
            var view = cam.GetViewMatrix();

            shader.SetMatrix4("uProjection", proj, false);
            shader.SetMatrix4("uInvProjection", invProj, false);
            shader.SetMatrix4("uView", view, true);

            shader.SetVector2("uResolution", new Vector2(clientSize.X, clientSize.Y));
            shader.SetFloat("uNear", cam.DepthNear);
            shader.SetFloat("uFar", cam.DepthFar);
            shader.SetFloat("uRadius", Radius);
            shader.SetFloat("uBias", Bias);
            shader.SetFloat("uIntensity", Intensity);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
        }

        static Vector3[] BuildKernel(int n)
        {
            var rng = new Random(1337);
            var list = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float x = (float)rng.NextDouble() * 2f - 1f;
                float y = (float)rng.NextDouble() * 2f - 1f;
                float z = (float)rng.NextDouble();
                var v = new Vector3(x, y, z).Normalized();
                float t = i / (float)n;
                float scale = 0.1f + (1.0f - 0.1f) * t * t;
                list[i] = v * scale;
            }
            return list;
        }
    }
}
