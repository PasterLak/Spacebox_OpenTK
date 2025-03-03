using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Utils;

namespace Engine
{
    public class Skybox : Node3D, ITransparent
    {
        public Mesh Mesh { get; private set; }
        public MaterialBase Material { get; private set; }
      
        public Texture2D Texture { get; private set; }

        public bool IsAmbientAffected = false; 

        public Skybox(string objPath, Shader shader, Texture2D texture)
        {
            var (vertices, indices) = ObjLoader.Load(objPath);
            Mesh = new Mesh(vertices, indices);
            Material = new TextureMaterial( texture);
            Material.RenderFace = RenderFace.Back;
            Texture = texture;
        

           Scale = new Vector3(100, 100, 100);
            Name = "Skybox";
          

        }
       
        public void DrawTransparent(Camera camera)
        {
            //Transform.Rotation += new Vector3(0,0.01f,0);
            Position = camera.Position ;
          
            bool cullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);

        
            //GL.Enable(EnableCap.Blend);

           // GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            GL.GetInteger(GetPName.CullFaceMode, out int prevCullFaceMode);
            GL.GetInteger(GetPName.DepthFunc, out int prevDepthFunc);

           // GL.DepthFunc(DepthFunction.Lequal);
           // GL.Disable(EnableCap.CullFace);
          //  GL.CullFace(CullFaceMode.FrontAndBack);

            Material.SetUniforms(GetModelMatrix());
           
           // Texture.Use();
            Material.Shader.SetInt("skybox", 0);

            
            //var viewMatrix = new Matrix4(new Matrix3(camera.GetViewMatrix()));
            //Material.Shader.SetVector2("offset", new Vector2(x,x) );
            if(IsAmbientAffected)
            Material.Shader.SetVector3("ambient", Lighting.AmbientColor);
            else
                Material.Shader.SetVector3("ambient", new Vector3(1, 1, 1));  // can be optimized

            //Material.Shader.SetMatrix4("view", camera.GetViewMatrix());
            //Material.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            //Material.Shader.SetMatrix4("model", GetModelMatrix());
            Material.Use();
            Mesh.Draw();

          
           // if (!cullFaceEnabled)
           //     GL.Disable(EnableCap.CullFace);
           // GL.CullFace((CullFaceMode)prevCullFaceMode);
           // GL.DepthFunc((DepthFunction)prevDepthFunc);
        }
    }
}
