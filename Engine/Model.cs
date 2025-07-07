using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Physics;



namespace Engine
{
    public class Model : StaticBody
    {
        public Mesh Mesh { get; private set; }
        public MaterialBase Material { get; private set; }

        public Model(Mesh mesh)
            : this( mesh, new ColorMaterial())
        {
            Name = "Model";
        }

        public Model(Mesh mesh, MaterialBase material)
        : base(new BoundingBox(Vector3.Zero, Vector3.One))
        {
           
            Mesh = mesh;
            Material = material;
            Name = "Model";
         
            Matrix4 modelMatrix = GetRenderModelMatrix();

            Vector3 worldMin = Vector3.TransformPosition(Mesh.GetBounds().Min, modelMatrix);
            Vector3 worldMax = Vector3.TransformPosition(Mesh.GetBounds().Max, modelMatrix);

            var b = BoundingBox.CreateFromMinMax(worldMin, worldMax);
            b.Size = b.Size * Scale;

            BoundingVolume = b;
           
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


        public void Render()
        {
            var cam = Camera.Main;
            
            if(cam == null) return;
            
            Material.Apply(this);
        
            //Material.Shader.SetMatrix4("model", GetModelMatrix());
            Material.Shader.SetMatrix4("view", cam.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", cam.GetProjectionMatrix());
            Mesh.Render();

        }
    }
}
