
using Engine.Light;
using OpenTK.Mathematics;

using System.Text;

namespace Engine.Graphics
{

    struct GlobalsGpu
    {
        public Vector3 AMBIENT; public float FOG_DENSITY;
        public Vector3 FOG; public float RANDOM01;
        public Vector3 CAMERA_POS; public float TIME;
        public Matrix4 VIEW;
        public Matrix4 PROJECTION;
    }

    public static class GlobalUniforms
    {

        const string GLOBALS = "GLOBALS";
        static readonly UniformBuffer<GlobalsGpu> _ubo = new UniformBuffer<GlobalsGpu>(GLOBALS);

        static readonly Random _random = new Random((int)(Time.Delta * 10));

        private static string Generate(string blockName, (string glslType, string fieldName)[] fields)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.Append("layout(std140) uniform ").Append(blockName).AppendLine();
            sb.AppendLine("{");
            foreach (var f in fields)
                sb.Append("    ").Append(f.glslType).Append(' ')
                  .Append(f.fieldName).AppendLine(";");
            sb.AppendLine("};");

          
            return sb.ToString();

        }

        public static string GenerateCode()
        {
            return Generate(
                GLOBALS,
            new[]
            {
            ("vec3",  nameof(GlobalsGpu.AMBIENT)),
            ("float", nameof(GlobalsGpu.FOG_DENSITY)),
            ("vec3",  nameof(GlobalsGpu.FOG)),
            ("float", nameof(GlobalsGpu.RANDOM01)),
            ("vec3",  nameof(GlobalsGpu.CAMERA_POS)),
            ("float", nameof(GlobalsGpu.TIME)),
            ("mat4",  nameof(GlobalsGpu.VIEW)),
            ("mat4",  nameof(GlobalsGpu.PROJECTION))
            });
        }

        public static void Push()
        {
            Camera cam = Camera.Main;
          
            GlobalsGpu g = default;

            
            g.AMBIENT = Lighting.AmbientColor;
            g.FOG_DENSITY = Lighting.FogDensity;
            g.FOG = Lighting.FogColor;
            g.RANDOM01 = _random.NextSingle();
            g.CAMERA_POS = cam != null ?
                Camera.Main.Position - RenderSpace.Origin : new Vector3(0);
            g.TIME = Time.Total;
            g.VIEW = cam != null ? Matrix4.Transpose(cam.GetViewMatrix()) : Matrix4.Identity;
            g.PROJECTION = cam != null ? Matrix4.Transpose(cam.GetProjectionMatrix()) : Matrix4.Identity;


            _ubo.Update(stackalloc GlobalsGpu[1] { g });
        }
    }
}
