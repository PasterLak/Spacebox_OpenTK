using Engine.Graphics;
using OpenTK.Mathematics;

namespace Engine.Light
{
    public enum LightKind { Directional, Point, Spot }

    public static class LightSystem
    {
        const int MAX_DIR = 4;
        const int MAX_POINT = 64;
        const int MAX_SPOT = 32;

        static readonly List<LightBase> _all = new();

        struct LightsCountGpu
        {
            public int dir; public int point;
            public int spot; public int pad;
        }

        struct DirGpu
        {
            public Vector3 direction; public float intensity;
            public Vector3 diffuse; public float pad2;
            public Vector3 specular; public float pad3;
        }

        struct PointGpu
        {
            public Vector3 position; public float constant;
            public Vector3 diffuse; public float quadratic;
            public Vector3 specular; public float linear;
            public float intensity; public float pad0, pad1, pad2;
        }

        struct SpotGpu
        {
            public Vector3 position; public float constant;
            public Vector3 direction; public float linear;
            public float innerCut; public float outerCut;
            public float quadratic; public float intensity;
            public Vector3 diffuse; public float pad2;
            public Vector3 specular; public float pad3;
        }

        static readonly UniformBuffer<LightsCountGpu> _cntUBO = new("LightsCountBlock");
        static readonly UniformBuffer<DirGpu> _dirUBO = new("DirBlock");
        static readonly UniformBuffer<PointGpu> _ptUBO = new("PointBlock");
        static readonly UniformBuffer<SpotGpu> _spUBO = new("SpotBlock");

        public static void Register(LightBase l) { if (!_all.Contains(l)) _all.Add(l); }
        public static void Unregister(LightBase l) { _all.Remove(l); }
        public static int GetRegisteredLightsCount => _all.Count;

        public const int DistanceToRenderLight = 128 * 128;
        public static void Clear() => _all.Clear();

        public static void Update()
        {

            var cam = Camera.Main;

            if (cam == null) return;

            Span<DirGpu> dir = stackalloc DirGpu[MAX_DIR];
            Span<PointGpu> pt = stackalloc PointGpu[MAX_POINT];
            Span<SpotGpu> sp = stackalloc SpotGpu[MAX_SPOT];



            int d = 0, p = 0, s = 0;

            for (int i = 0; i < _all.Count; ++i)
            {
                var l = _all[i];
                if (!l.Enabled || l.Intensity <= 0) continue;

                switch (l.Kind)
                {
                    case LightKind.Directional when d < MAX_DIR:
                        var dl = (DirectionalLight)l;
                        dir[d++] = new DirGpu
                        {
                            direction = Vector3.Normalize(dl.Direction),
                            intensity = dl.Intensity,
                            diffuse = dl.Diffuse,
                            specular = dl.Specular
                        };
                        break;

                    case LightKind.Point when p < MAX_POINT:


                        var pl = (PointLight)l; float k = pl.Intensity;
                        var camPos = cam.GetWorldPosition();
                        var lightPos = pl.GetWorldPosition();
                        if ((lightPos - camPos).LengthSquared >= DistanceToRenderLight)
                            continue;
                        pt[p++] = new PointGpu
                        {
                            position = pl.GetWorldPosition() - RenderSpace.Origin,
                            constant = pl.Constant,
                            linear = pl.Linear,
                            quadratic = pl.Quadratic,
                            intensity = pl.Intensity,
                            diffuse = pl.Diffuse * k,
                            specular = pl.Specular * k
                        };
                        break;

                    case LightKind.Spot when s < MAX_SPOT:
                        var sl = (SpotLight)l;
                        var camPos2 = cam.GetWorldPosition();
                        var lightPos2 = sl.GetWorldPosition();
                        if ((lightPos2 - camPos2).LengthSquared >= DistanceToRenderLight)
                            continue;
                        sp[s++] = new SpotGpu
                        {
                            position = sl.GetWorldPosition() - RenderSpace.Origin,
                            constant = sl.Constant,
                            direction = Vector3.Normalize(sl.Direction),
                            linear = sl.Linear,
                            innerCut = sl.CutOff,
                            outerCut = sl.OuterCutOff,
                            quadratic = sl.Quadratic,
                            intensity = sl.Intensity,
                            diffuse = sl.Diffuse,
                            specular = sl.Specular
                        };
                        break;
                }
            }

            _cntUBO.Update(stackalloc LightsCountGpu[1] { new LightsCountGpu { dir = d, point = p, spot = s } });
            _dirUBO.Update(dir[..d]);
            _ptUBO.Update(pt[..p]);
            _spUBO.Update(sp[..s]);
        }
    }
}
