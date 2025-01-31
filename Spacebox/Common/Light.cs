﻿using OpenTK.Mathematics;
using Spacebox.FPS;


namespace Spacebox.Common
{
    public abstract class Light : Node3D, INotTransparent
    {
        public Shader Shader { get; set; }

        public abstract Vector3 Ambient { get; set; }
        public abstract Vector3 Diffuse { get; set; }
        public abstract Vector3 Specular { get; set; }

        public Light(Shader shader)
        {
            Shader = shader;
        }

        public virtual void Draw(Camera camera)
        {
            if (Shader == null) return;
            Shader.Use();
            Shader.SetMatrix4("view", camera.GetViewMatrix());
            Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            Shader.SetVector3("viewPos", camera.Position);
        }
    }

}
