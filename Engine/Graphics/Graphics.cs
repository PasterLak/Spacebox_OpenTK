using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public static class GraphicsBase
    {
        public static IGraphicsAPI API { get; private set; } = new GL4GraphicsAPI();

        public static void Use(IGraphicsAPI api) => API = api;

        public static void Clear(ClearBufferMask mask) => API.Clear(mask);
        public static void ClearColor(float r, float g, float b, float a) => API.ClearColor(r, g, b, a);
        public static void DrawElements(PrimitiveType mode, int count,
                                        DrawElementsType type, int offset) => API.DrawElements(mode, count, type, offset);
        public static void DrawArrays(PrimitiveType mode, int first, int count) => API.DrawArrays(mode, first, count);
        public static void Enable(EnableCap cap) => API.Enable(cap);
        public static void Disable(EnableCap cap) => API.Disable(cap);
        public static void Viewport(int x, int y, int width, int height) => API.Viewport(x, y, width, height);
        public static void LineWidth(float width) => API.LineWidth(width);
        public static void BindVertexArray(int vao) => API.BindVertexArray(vao);
        public static void BindBuffer(BufferTarget target, int buffer) => API.BindBuffer(target, buffer);
        public static void BufferData(BufferTarget target, int size,
                                      System.IntPtr data, BufferUsageHint usage) => API.BufferData(target, size, data, usage);
    }

    public interface IGraphicsAPI
    {
        void Clear(ClearBufferMask mask);
        void ClearColor(float r, float g, float b, float a);
        void DrawElements(PrimitiveType mode, int count, DrawElementsType type, int offset);
        void DrawArrays(PrimitiveType mode, int first, int count);
        void Enable(EnableCap cap);
        void Disable(EnableCap cap);
        void Viewport(int x, int y, int width, int height);
        void LineWidth(float width);
        void BindVertexArray(int vao);
        void BindBuffer(BufferTarget target, int buffer);
        void BufferData(BufferTarget target, int size, System.IntPtr data, BufferUsageHint usage);
    }

    public sealed class GL4GraphicsAPI : IGraphicsAPI
    {
        public void Clear(ClearBufferMask mask) => GL.Clear(mask);
        public void ClearColor(float r, float g, float b, float a) => GL.ClearColor(r, g, b, a);
        public void DrawElements(PrimitiveType mode, int count,
                                 DrawElementsType type, int offset) => GL.DrawElements(mode, count, type, offset);
        public void DrawArrays(PrimitiveType mode, int first, int count) => GL.DrawArrays(mode, first, count);
        public void Enable(EnableCap cap) => GL.Enable(cap);
        public void Disable(EnableCap cap) => GL.Disable(cap);
        public void Viewport(int x, int y, int width, int height) => GL.Viewport(x, y, width, height);
        public void LineWidth(float width) => GL.LineWidth(width);
        public void BindVertexArray(int vao) => GL.BindVertexArray(vao);
        public void BindBuffer(BufferTarget target, int buffer) => GL.BindBuffer(target, buffer);
        public void BufferData(BufferTarget target, int size,
                               System.IntPtr data, BufferUsageHint usage) => GL.BufferData(target, size, data, usage);
    }
}
