﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Skybox : IDrawable
    {
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }
        public Transform Transform { get; private set; }
        public Texture2D Texture { get; private set; }

        public Skybox(string objPath, Shader shader, Texture2D texture)
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);
            Material = new Material(shader, texture);
            Texture = texture;
            Transform = new Transform();

            Transform.Scale = new Vector3(100, 100, 100);
        }
       
        public void Draw(Camera camera)
        {
            //Transform.Rotation += new Vector3(0,0.01f,0);
            Transform.Position = camera.Position;
            // Save previous OpenGL state
            bool cullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);
            GL.GetInteger(GetPName.CullFaceMode, out int prevCullFaceMode);
            GL.GetInteger(GetPName.DepthFunc, out int prevDepthFunc);

            // Set OpenGL state for skybox rendering
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Disable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.FrontAndBack); // Cull front faces to render inside of the cube

            Material.Use();
            Texture.Use();
            Material.Shader.SetInt("skybox", 0);
            
            // Use view matrix without translation
            //var viewMatrix = new Matrix4(new Matrix3(camera.GetViewMatrix()));
            //Material.Shader.SetVector2("offset", new Vector2(x,x) );
            Material.Shader.SetMatrix4("view", camera.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            Material.Shader.SetMatrix4("model", Transform.GetModelMatrix());

            Mesh.Draw();

            // Restore previous OpenGL state
            if (!cullFaceEnabled)
                GL.Disable(EnableCap.CullFace);
            GL.CullFace((CullFaceMode)prevCullFaceMode);
            GL.DepthFunc((DepthFunction)prevDepthFunc);
        }
    }
}
