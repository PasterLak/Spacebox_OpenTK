using OpenTK.Graphics.OpenGL4;

namespace Spacebox.Common
{
    public class Model : IDrawable
    {
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }
        public Transform Transform { get; private set; }
        public bool BackfaceCulling { get; set; } = true;
        public Collision Collision { get; private set; }
        private Axes _axes;

        public Model(string objPath)
        {
            Init(objPath, new Material());
        }

        public Model(string objPath, Material material)
        {
            Init(objPath, material);
        }

        private void Init(string objPath, Material material)
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);
            Material = material;
            Transform = new Transform();
            var boundingBox = new BoundingBox(Transform.Position, Mesh.GetBounds().Size);
            Collision = new Collision(Transform, boundingBox, true);
            _axes = new Axes(Transform.Position, boundingBox.GetLongestSide() - boundingBox.GetLongestSide() / 4);
            ComputeBoundingVolumes();
        }

        private void ComputeBoundingVolumes()
        {
            Collision.UpdateBounding();
        }

        public void UpdateBounding()
        {
            ComputeBoundingVolumes();
        }

        public void Draw(Camera camera)
        {
            if (BackfaceCulling)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            if (Debug.ShowDebug)
            {
                Collision.DrawDebug();
                _axes.SetPosition(Transform.Position);
                _axes.SetRotation(Transform.Rotation);
                _axes.Render(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            }

            Material.Use();
            Material.Shader.SetMatrix4("model", Transform.GetModelMatrix());
            Material.Shader.SetMatrix4("view", camera.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            Mesh.Draw();
        }
    }
}
