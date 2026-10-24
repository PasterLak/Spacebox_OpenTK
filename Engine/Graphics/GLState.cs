using Engine.Graphics.Rhi;

namespace Engine.Graphics
{
    public static class GLState
    {
        public const int GBufferAttachment = 1;

        public static void GBufferWrite(bool enable) => Gpu.Device.SetColorWrite(GBufferAttachment, enable);

        public static void DepthTest(bool enable) => Gpu.Device.SetDepthTest(enable);

        public static void DepthMask(bool enable) => Gpu.Device.SetDepthWrite(enable);

        public static void Blend(bool enable) => Gpu.Device.SetBlend(enable);

        public static void CullFace(bool enable) => Gpu.Device.SetCulling(enable);

        public static void CullMode(Rhi.CullFace mode) => Gpu.Device.SetCullFace(mode);

        public static void BlendFunc(BlendFactor src, BlendFactor dst) => Gpu.Device.SetBlendFunction(src, dst);

        public static void InvalidateBlendFunc() => Gpu.Device.InvalidateBlendFunc();
    }
}
