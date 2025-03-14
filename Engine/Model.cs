﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Physics;
using Engine.Utils;


namespace Engine
{
    public class Model : StaticBody, INotTransparent
    {
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }
        public bool BackfaceCulling { get; set; } = true;
        private Axes _axes;

        public Model(Mesh mesh)
            : this( mesh, new Material())
        {
        }

        public Model(Mesh mesh, Material material)
        : base(new BoundingBox(Vector3.Zero, Vector3.One))
        {
           
            Mesh = mesh;
            Material = material;

         
            Matrix4 modelMatrix = GetModelMatrix();

            Vector3 worldMin = Vector3.TransformPosition(Mesh.GetBounds().Min, modelMatrix);
            Vector3 worldMax = Vector3.TransformPosition(Mesh.GetBounds().Max, modelMatrix);

            var b = BoundingBox.CreateFromMinMax(worldMin, worldMax);
            b.Size = b.Size * Scale;

            BoundingVolume = b;
            _axes = new Axes(Position, BoundingVolume.GetLongestSide() * 2);
            //UpdateBounding();

            oldColor = Material.Color;
            //Name = GetModelName(objPath);
        }


        private Color4 oldColor;
        public override void OnCollisionEnter(Collision other)
        {

            
            if (other is DynamicBody)
            {
                oldColor = Material.Color;
                Material.Color = new Color4(0, 1, 0, 1f);
                base.OnCollisionEnter(other);
            }
        }

        public override void OnCollisionExit(Collision other)
        {
            
            if (other is DynamicBody)
            {
                Material.Color = oldColor;
                base.OnCollisionExit(other);
            }
        }

        public static string GetModelName(string modelPath)
        {
            if (string.IsNullOrEmpty(modelPath))
                return "Error";

            return Path.GetFileNameWithoutExtension(modelPath);
        }

       

        public void Render(Camera camera)
        {
            if (BackfaceCulling)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            if (VisualDebug.Enabled)
            {
              
                _axes.SetPosition(Position);
                _axes.SetRotation(Rotation);
                _axes.Render(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            }

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);

            Material.Use();
            Material.Shader.SetMatrix4("model", GetModelMatrix());
            Material.Shader.SetMatrix4("view", camera.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            Mesh.Render();

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

        }
    }
}
