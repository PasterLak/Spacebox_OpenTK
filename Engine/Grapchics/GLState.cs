using OpenTK.Graphics.OpenGL4;

namespace Engine.Graphics
{
    public static class GLState
    {
        private static bool _depthTest = true;
        private static bool _depthWrite = true;
        private static bool _blend = false;
        private static bool _cullFace = false;
        private static CullFaceMode _cullMode = CullFaceMode.Back;
        private static BlendingFactor _srcBlend = BlendingFactor.One;
        private static BlendingFactor _dstBlend = BlendingFactor.Zero;

        public static void DepthTest(bool enable)
        {
            if (_depthTest != enable)
            {
                if (enable) GL.Enable(EnableCap.DepthTest);
                else GL.Disable(EnableCap.DepthTest);
                _depthTest = enable;
            }
        }

        public static void DepthMask(bool enable)
        {
            if (_depthWrite != enable)
            {
                GL.DepthMask(enable);
                _depthWrite = enable;
            }
        }

        public static void Blend(bool enable)
        {
            if (_blend != enable)
            {
                if (enable) GL.Enable(EnableCap.Blend);
                else GL.Disable(EnableCap.Blend);
                _blend = enable;
            }
        }

        public static void CullFace(bool enable)
        {
            if (_cullFace != enable)
            {
                if (enable) GL.Enable(EnableCap.CullFace);
                else GL.Disable(EnableCap.CullFace);
                _cullFace = enable;
            }
        }

        public static void CullMode(CullFaceMode mode)
        {
            if (_cullMode != mode)
            {
                GL.CullFace(mode);
                _cullMode = mode;
            }
        }

        public static void BlendFunc(BlendingFactor src, BlendingFactor dst)
        {
            if (_srcBlend != src || _dstBlend != dst)
            {
                GL.BlendFunc(src, dst);
                _srcBlend = src;
                _dstBlend = dst;
            }
        }
    }
}