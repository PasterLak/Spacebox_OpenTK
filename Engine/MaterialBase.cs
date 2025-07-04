﻿
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public enum RenderMode : byte
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
    }
    public enum RenderFace : byte
    {
        Both,
        Front,
        Back
    }
    public abstract class MaterialBase
    {
        public Shader Shader { get;  set; }

        public int RenderQueue { get; protected set; } = 1000;
        public RenderMode RenderMode { get; protected set; } = RenderMode.Opaque;
        public RenderFace RenderFace { get;  set; } = RenderFace.Both;

        public Color4 Color { get; set; } = Color4.White;

        private byte textureUniformID = 0;

        public MaterialBase(Shader shader)
        {
            Shader = shader;
        }

        private void SetMVP(Matrix4 modelMatrix)
        {
            Shader.SetMatrix4("model", modelMatrix);
            var camera = Camera.Main;
            if (camera == null) return;

            Shader.SetMatrix4("view", camera.GetViewMatrix());
            Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
        }

        private void SetFaceCullingMode(RenderFace face)
        {
            if (face == RenderFace.Both)
            {
                GL.Disable(EnableCap.CullFace);
                return;
            }

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(face == RenderFace.Front ? CullFaceMode.Back : CullFaceMode.Front);
        }

        private void SetRenderMode(RenderMode mode)
        {
            bool enableDepthTest = mode != RenderMode.Transparent;
            bool enableBlending = mode == RenderMode.Fade || mode == RenderMode.Transparent;
            bool depthWrite = mode == RenderMode.Opaque || mode == RenderMode.Cutout;
            if (enableDepthTest) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.DepthTest);
            //if (mode == RenderMode.Transparent)
            //    GL.DepthMask(false);
            // else
            //    GL.DepthMask(true);

            //GL.DepthMask(depthWrite);


            if (enableBlending)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
            else
            {
                GL.Disable(EnableCap.Blend);
            }
        }

        protected void UseTexture(Texture2D? texture, string uniformName)
        {
            if (texture != null)
            {

                texture.Use(TextureUnit.Texture0 + textureUniformID);
                Shader.SetInt(uniformName, textureUniformID);

                textureUniformID++;
            }
        }

        public virtual void SetUniforms(Matrix4 modelMatrix)
        {
            textureUniformID = 0;
            Shader.Use();
            ApplyRenderSettings();
            SetMVP(modelMatrix);
            SetMaterialProperties();
        }

        protected virtual void ApplyRenderSettings()
        {
            SetFaceCullingMode(RenderFace);
            SetRenderMode(RenderMode);
        }

        protected virtual void SetMaterialProperties()
        {
            //Shader.Use();

            Shader.SetVector4("color", Color);
        }

        public virtual void Use()
        {
            Shader.Use();
        }

    }
}
