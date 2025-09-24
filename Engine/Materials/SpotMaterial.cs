using Engine.Light;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Data;

namespace Engine
{
    public class SpotMaterial : MaterialBase
    {
        public SpotLight Light { get; }
        public float Shininess { get; set; } = 48f;

        public SpotMaterial(SpotLight light) : base(Resources.Load<Shader>("Resources/Shaders/spot"))
        {
            Light = light;
            RenderMode = RenderMode.Transparent;
            RenderFace = RenderFace.Both;

          

        }

        protected override void ApplyRenderSettings()
        {
            base.ApplyRenderSettings();

        }

        public Vector3 RotToNorm(Vector3 v)
        {
            float pitch = MathHelper.DegreesToRadians(v.X);
            float yaw = MathHelper.DegreesToRadians(v.Y);
            float roll = MathHelper.DegreesToRadians(v.Z);


            Quaternion q = Quaternion.FromEulerAngles(pitch, yaw, roll);

            Vector3 forward = Vector3.Transform(-Vector3.UnitZ, q);
            forward = forward.Normalized();
            return forward;
        }
        protected override void UpdateDynamicUniforms()
        {
            if (Light == null)
            {
                return;
            }
            Shader.SetFloat("lightLength", 1);

            Shader.SetFloat("innerAngle", Light.CutOff);
            Shader.SetFloat("outerAngle", Light.OuterCutOff);
            // Shader.SetFloat("fadeStart", 1);
            // Shader.SetFloat("fadeEnd", 1);
         
            Shader.SetVector3("lightColor", Light.Diffuse);

            Shader.SetVector3("lightDir", Light.Direction);
            Shader.SetVector3("lightPos", Light.PositionWorld);
          
        }

        public static MeshBuffer GetMeshBuffer()
        {
            var attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "vertexPosition", Size = 3 },
                new BufferAttribute { Name = "vertexNormal",   Size = 3 },
                new BufferAttribute { Name = "vertexUV",       Size = 2 }
            };
            return new MeshBuffer(attrs);
        }
    }
}
