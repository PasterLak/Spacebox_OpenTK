using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Model : IDrawable
    {
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }
        public Transform Transform { get; private set; }
        public bool BackfaceCulling { get; set; } = true;

        public Model(string objPath, Material material)
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);
            Material = material;
            Transform = new Transform();
        }

        public void Draw(Camera camera)
        {
            if (BackfaceCulling)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            Debug.DrawTransform(Transform);

            Material.Use();
            Material.Shader.SetMatrix4("model", Transform.GetModelMatrix());
            Material.Shader.SetMatrix4("view", camera.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            Mesh.Draw();
        }
    }
}
