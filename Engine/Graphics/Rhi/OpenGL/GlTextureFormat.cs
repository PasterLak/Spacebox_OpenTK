using OpenTK.Graphics.OpenGL4;

namespace Engine.Graphics.Rhi.OpenGL;

internal readonly record struct GlTextureFormat(PixelInternalFormat InternalFormat, PixelFormat Format, PixelType Type);
