using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Model : StaticBody, IDrawable
    {
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }
        public bool BackfaceCulling { get; set; } = true;
        private Axes _axes;

        public Model(string objPath)
            : this(objPath, new Material())
        {
        }

        public Model(string objPath, Material material)
            : base(new Transform(), new BoundingBox(Vector3.Zero, Vector3.One))
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);
            Material = material;
            Transform = new Transform();
            BoundingVolume = new BoundingBox(Transform.Position, Mesh.GetBounds().Size);
            _axes = new Axes(Transform.Position, BoundingVolume.GetLongestSide() - BoundingVolume.GetLongestSide() / 4);
            ComputeBoundingVolumes();
        }

        private void ComputeBoundingVolumes()
        {
            UpdateBounding();
        }

        public override void OnCollisionEnter(Collision other)
        {
            if (other is DynamicBody)
            {
                Material.Color = new Vector4(1, 0, 0, 1);
            }
        }

        public override void OnCollisionExit(Collision other)
        {
            if (other is DynamicBody)
            {
                Material.Color = new Vector4(1, 1, 1, 1);
            }
        }

        public void Draw(Camera camera)
        {
            if (BackfaceCulling)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            if (Debug.ShowDebug)
            {
                DrawDebug();
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
