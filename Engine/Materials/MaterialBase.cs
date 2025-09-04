
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

        public bool TransposeMatrices = true;

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
        public void ReplaceTexture(string uniform, Texture2D newTexture)
        {
            if (newTexture == null || string.IsNullOrEmpty(uniform)) return;

            if (_slots.ContainsKey(uniform))
            {
                var currentUnit = _slots[uniform].unit;
                _slots[uniform] = (currentUnit, newTexture);
            }
            else
            {
                AddTexture(uniform, newTexture);
            }
        }

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

        private void _setMVP(Matrix4 model)
        {
            var cam = Camera.Main;
            Shader.SetMatrix4("model", model, TransposeMatrices);
            if (cam == null) return;
            var view = cam.GetViewMatrix();
            var proj = cam.GetProjectionMatrix();

            Shader.SetMatrix4("view", view, TransposeMatrices);
            Shader.SetMatrix4("projection", proj, TransposeMatrices);
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
            bool depthTest          = mode != RenderMode.Transparent;
            bool enableBlending     = mode == RenderMode.Fade || mode == RenderMode.Transparent;
            bool depthWrite         = mode == RenderMode.Opaque || mode == RenderMode.Cutout;

            if (depthTest)  GL.Enable(EnableCap.DepthTest);
            else            GL.Disable(EnableCap.DepthTest);
           
            GL.DepthMask(depthWrite);

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
        

    }
}
