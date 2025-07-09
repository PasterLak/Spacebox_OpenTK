
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public sealed class SkyboxProceduralMaterial : MaterialBase
    {
        Vector3 _sunDir = new(-0.8f, 0.15f, -0.3f);
        public SkyboxProceduralMaterial()
            : base(Resources.Load<Shader>("Shaders/procedural_skybox"))
        {
            RenderFace = RenderFace.Back;
            RenderMode = RenderMode.Opaque;
        }
        public Vector3 SunDirection { get => _sunDir; set => _sunDir = value.Normalized(); }

        protected override void ApplyRenderSettings()
        {
            base.ApplyRenderSettings();

            GL.Disable(EnableCap.DepthTest); // никакого depth-test’а
            GL.DepthMask(false);             // и записи глубины
            GL.Disable(EnableCap.Blend);     // без смешения
           
        }

        protected override void UpdateDynamicUniforms()
        {
            
            Shader.SetVector3("SUN_DIR", _sunDir);
          
        }
    }
}
