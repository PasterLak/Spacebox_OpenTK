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
            public float intensity; public float pad0; public float pad1; public float pad2;
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

        static readonly UniformBuffer<DirGpu> _dirUBO = new UniformBuffer<DirGpu>("DirBlock");
        static readonly UniformBuffer<PointGpu> _pointUBO = new UniformBuffer<PointGpu>("PointBlock");
        static readonly UniformBuffer<SpotGpu> _spotUBO = new UniformBuffer<SpotGpu>("SpotBlock");

        public static void Register(LightBase l) { 
            if (!_all.Contains(l))
            {
                Debug.Success("Register light " + l.Kind);
                _all.Add(l);

            }

        }
        public static void Unregister(LightBase l) {
            Debug.Warning("Unregister light " + l.Kind);
            _all.Remove(l);
           
        }

        public static int GetLightsCount => _all.Count;

        public static void Clear() => _all.Clear();

        static void AddLight<T>(in T src, Span<T> dst, ref int count) where T : struct
        {
            if (count < dst.Length) dst[count++] = src;
        }

        public static void Update()
        {
            Span<DirGpu> dir = stackalloc DirGpu[MAX_DIR];
            Span<PointGpu> pt = stackalloc PointGpu[MAX_POINT];
            Span<SpotGpu> sp = stackalloc SpotGpu[MAX_SPOT];

            int d = 0, p = 0, s = 0;

            foreach (var l in _all)
            {
                if (!l.Enabled) continue;
                if (l.Intensity <= 0) continue;

                switch (l.Kind)
                {
                    case LightKind.Directional:
                        var dl = (DirectionalLight)l;
                        AddLight(new DirGpu
                        {
                            direction = Vector3.Normalize(dl.Direction),
                            intensity = dl.Intensity,

                            diffuse = dl.Diffuse,
                            specular = dl.Specular
                        }, dir, ref d);
                        break;

                    case LightKind.Point:
                        var pl = (PointLight)l;
                        float k = pl.Intensity;
                        AddLight(new PointGpu
                        {
                            position = pl.LocalToWorld(Vector3.Zero) - RenderSpace.Origin,
                           // position = pl.GetWorldPosition() - RenderSpace.Origin,
                            constant = pl.Constant,
                            intensity = pl.Intensity,
                            linear = pl.Linear,
                            diffuse = pl.Diffuse * k,
                            quadratic = pl.Quadratic,
                            specular = pl.Specular * k
                        }, pt, ref p);
                        break;

                    case LightKind.Spot:
                        var sl = (SpotLight)l;
                        AddLight(new SpotGpu
                        {
                            //position = sl.LocalToWorld(Vector3.Zero) - RenderSpace.Origin,
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
                        }, sp, ref s);
                        break;
                }
            }

            _dirUBO.Update(dir.Slice(0, d));
            _pointUBO.Update(pt.Slice(0, p));
            _spotUBO.Update(sp.Slice(0, s));
        }
    }
}
