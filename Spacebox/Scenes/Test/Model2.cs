using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Physics;
using Engine.Utils;
using Engine;
using Spacebox.Scenes.Test;

public class Model2 : SceneNode,  IGameComponent
    {
        public Mesh Mesh { get; private set; }
        public MaterialBase Material { get; private set; }
        public bool BackfaceCulling { get; set; } = true;
        private Axes2 _axes;

        public Model2(string objPath)
            : this(objPath, new ColorMaterial())
        {
        }

        public Model2(string objPath, MaterialBase material)
       
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
        Mesh = Resources.Get<Mesh>(objPath);
            Material = material;


            Matrix4 modelMatrix = WorldMatrix;

            Vector3 worldMin = Vector3.TransformPosition(Mesh.GetBounds().Min, modelMatrix);
            Vector3 worldMax = Vector3.TransformPosition(Mesh.GetBounds().Max, modelMatrix);

            _axes = new Axes2(Position, Scale.X*2f);
        _axes.Thickness = 0.1f;

        AddChild(_axes);
            //UpdateBounding();

           // oldColor = Material.Color;
            Name = GetModelName(objPath);
        }


        private Vector4 oldColor;
       

        public static string GetModelName(string modelPath)
        {
            if (string.IsNullOrEmpty(modelPath))
                return "Error";

            return Path.GetFileNameWithoutExtension(modelPath);
        }



        public void Render()
        {
            if (BackfaceCulling)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

        var camera = Camera.Main;

            if (VisualDebug.Enabled)
            {

              //  _axes.SetPosition(Position);
              // _axes.SetRotation(Rotation);
                _axes.RenderAxes(camera);
            }

        // GL.Enable(EnableCap.DepthTest);
        // GL.Enable(EnableCap.Blend);
            Material.RenderFace = RenderFace.Front;
            Material.SetUniforms(WorldMatrix);

            Material.Use();
            /*Material.Shader.SetMatrix4("model", WorldMatrix);
            Material.Shader.SetMatrix4("view", camera.GetViewMatrix());
            Material.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());*/
            Mesh.Draw();

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

        }

}

