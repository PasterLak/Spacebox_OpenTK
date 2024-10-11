using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class ModelLocal : StaticBody, INotTransparent
    {
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }
        public bool BackfaceCulling { get; set; } = true;
        private Axes _axes;

        public ModelLocal(string objPath)
            : this(objPath, new Material())
        {
        }

        public ModelLocal(string objPath, Material material)
        : base(new BoundingBox(Vector3.Zero, Vector3.One))
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);
            Material = material;

            // Корректное вычисление BoundingVolume с учётом позиции и масштаба
            Matrix4 modelMatrix = GetModelMatrix();

            Vector3 worldMin = Vector3.TransformPosition(Mesh.GetBounds().Min, modelMatrix);
            Vector3 worldMax = Vector3.TransformPosition(Mesh.GetBounds().Max, modelMatrix);

            var b = BoundingBox.CreateFromMinMax(worldMin, worldMax);
            b.Size = b.Size * Scale;

            BoundingVolume = b;
            _axes = new Axes(Position, BoundingVolume.GetLongestSide() * 2);
            //UpdateBounding();

            oldColor = Material.Color;
            Name = GetModelName(objPath);
        }


        private Vector4 oldColor;
        public override void OnCollisionEnter(Collision other)
        {


            if (other is DynamicBody)
            {
                oldColor = Material.Color;
                Material.Color = new Vector4(0, 1, 0, 1f);
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

        public void Draw(Camera camera)
        {
            if (BackfaceCulling)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);



            if (Debug.ShowDebug)
            {

                _axes.SetPosition(Position);
                _axes.SetRotation(Rotation);
                _axes.Render(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            }

            // Рассчитываем относительные координаты объекта относительно игрока
            Matrix4 model = Matrix4.CreateTranslation(Position-camera.Position);
            Matrix4 mvp = model * camera.GetViewMatrix() * camera.GetProjectionMatrix();

            Material.Use();
            Material.Shader.SetMatrix4("model", mvp);
            Material.Shader.SetMatrix4("view", camera.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            Mesh.Draw();
        }
    }
}
