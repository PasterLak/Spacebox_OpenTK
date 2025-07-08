
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public enum RenderMode : byte { Opaque, Cutout, Fade, Transparent }
    public enum RenderFace : byte { Both, Front, Back }
        public class MaterialBase
    {
        public Shader Shader { get; }
        public Color4 Color { get; set; } = Color4.White;
        public bool UseFog { get; set; } = true;
        public bool UseAmbientLight { get; set; } = true;
        public RenderMode RenderMode { get; set; } = RenderMode.Opaque;
        public RenderFace RenderFace { get; set; } = RenderFace.Both;
        public int RenderQueue { get; set; } = 1000;

        readonly Dictionary<string, (TextureUnit unit, Texture2D tex)> _slots = new();
        readonly Dictionary<string, object> _parameters = new();
        readonly int _maxUnits = GL.GetInteger(GetPName.MaxCombinedTextureImageUnits);


        public MaterialBase(Shader shader)
            => Shader = shader ?? throw new ArgumentNullException(nameof(shader));

        public void AddTexture(string uniform, Texture2D tex)
        {
            if (tex == null || string.IsNullOrEmpty(uniform)) return;
            if (_slots.ContainsKey(uniform)) _slots[uniform] = (_slots[uniform].unit, tex);
            else
            {
                int n = _slots.Count;
                if (n >= _maxUnits) throw new InvalidOperationException("Too many textures");
                _slots[uniform] = (TextureUnit.Texture0 + n, tex);
            }
        }

        //public void Set<T>(string uniform, T value) => _parameters[uniform] = value!;

        public void Apply(Node3D node) => Apply(node.GetRenderModelMatrix());

        public void Apply(Matrix4 model)
        {
            Shader.Use();
            //_applyState();
            ApplyRenderSettings();
            _bindTextures();
           
            _setMVP(model);
            _setFixed();
            UpdateDynamicUniforms();
            // _setUser();
        }

        protected virtual void UpdateDynamicUniforms() { }

          void _bindTextures()
        {
            foreach (var (name, slot) in _slots)
            {
                slot.tex.Use(slot.unit);
                Shader.SetInt(name, (int)(slot.unit - TextureUnit.Texture0));
            }
        }

        private void _setFixed()  // basic variables for materials
        {
            Shader.SetVector4("color", Color);  
        }

        private void _setUser()
        {
            foreach (var (name, val) in _parameters)
            {
                switch (val)
                {
                    case int i: Shader.SetInt(name, i); break;
                    case float f: Shader.SetFloat(name, f); break;
                    case Vector2 v2: Shader.SetVector2(name, v2); break;
                    case Vector3 v3: Shader.SetVector3(name, v3); break;
                    case Vector4 v4: Shader.SetVector4(name, v4); break;
                    case Matrix4 m4: Shader.SetMatrix4(name, m4, false); break;
                    default: throw new NotSupportedException(val.GetType().Name);
                }
            }
        }

        private void _setMVP(Matrix4 model)
        {
            var cam = Camera.Main;
            Shader.SetMatrix4("model", model);
            if (cam == null) return;
            var view = cam.GetViewMatrix();
            var proj = cam.GetProjectionMatrix();
            
            Shader.SetMatrix4("view", view);
            Shader.SetMatrix4("projection", proj);
        }
        protected virtual void ApplyRenderSettings()
        {
            SetFaceCullingMode(RenderFace);
            SetRenderMode(RenderMode);
        }
        private void SetFaceCullingMode(RenderFace face)
        {
            if (face == RenderFace.Both)
            {
                GL.Disable(EnableCap.CullFace);
                return;
            }

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(face == RenderFace.Front ? CullFaceMode.Back : CullFaceMode.Front);
        }
        private void SetRenderMode(RenderMode mode)
        {
            bool enableDepthTest = mode != RenderMode.Transparent;
            bool enableBlending = mode == RenderMode.Fade || mode == RenderMode.Transparent;
            bool depthWrite = mode == RenderMode.Opaque || mode == RenderMode.Cutout;
            if (enableDepthTest) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.DepthTest);
         

            //GL.DepthMask(RenderMode == RenderMode.Opaque || RenderMode == RenderMode.Cutout);
      

            if (enableBlending)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
            else
            {
                GL.Disable(EnableCap.Blend);
            }
        }
        private void _applyState()
        {
            if (RenderFace == RenderFace.Both) GL.Disable(EnableCap.CullFace);
            else { GL.Enable(EnableCap.CullFace); GL.CullFace(RenderFace == RenderFace.Front ? CullFaceMode.Back : CullFaceMode.Front); }

            bool depthTest = RenderMode != RenderMode.Transparent;
            bool depthWrite = RenderMode == RenderMode.Opaque || RenderMode == RenderMode.Cutout;
            bool blend = RenderMode == RenderMode.Fade || RenderMode == RenderMode.Transparent;

            if (depthTest) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(depthWrite);

            if (blend) { GL.Enable(EnableCap.Blend); GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); }
            else GL.Disable(EnableCap.Blend);
        }
    }
}
