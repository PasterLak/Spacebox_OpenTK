using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Model : IDrawable, ICollidable
    {
        public bool IsStatic { get; } = true;
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }

        private BoundingBox _boundingBox;
        public BoundingBox BoundingBox { 
            get {
                _boundingBox.Center = Transform.Position;
                _boundingBox.Size =  _boundingBox.Size;
                return _boundingBox; 
            }
                private set {
                _boundingBox = value;
            }
        }
        public Transform Transform { get; private set; }
        public bool BackfaceCulling { get; set; } = true;


        public BoundingVolume BoundingVolume => BoundingBox;

        private Axes _axes;

        public Model(string objPath)
        {
       
            Init(objPath,new Material());
           
        }

        public Model(string objPath, Material material)
        {
            Init( objPath,material);
      
        }

        private void Init(string objPath, Material material)
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);

            Material = material;
            Transform = new Transform();
            BoundingBox = new BoundingBox(Transform.Position, Mesh.GetBounds().Size );
          
            _axes = new Axes(Transform.Position, BoundingBox.GetLongestSide() - BoundingBox.GetLongestSide()/4);

            ComputeBoundingVolumes();
        }

        private void ComputeBoundingVolumes()
        {
           
            BoundingBox = new BoundingBox(Transform.Position, Mesh.GetBounds().Size);
           
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
                Debug.DrawBoundingBox(BoundingBox, Color4.Yellow);
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
        private HashSet<ICollidable> _currentColliders = new HashSet<ICollidable>();
        public void OnCollisionEnter(ICollidable other)
        {
            if (_currentColliders.Contains(other))
                return;
            _currentColliders.Add(other);
           
            Material.Color = new Vector4(1, 0, 0, 1); 
        }

        public void OnCollisionExit(ICollidable other)
        {
            if (_currentColliders.Contains(other))
            {
                Material.Color = new Vector4(1, 1, 1, 1);

                _currentColliders.Remove(other);
               
            }
        }
    }
}
