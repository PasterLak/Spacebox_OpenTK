using Engine.Audio;
using Engine.Utils;
using OpenTK.Mathematics;

namespace Engine;

public static class ResourceFallbacks
{
    private const string ShaderCode = "--Vert\r\n#version 330 core\r\nlayout (location = 0) in vec3 aPos;\r\nlayout (location = 1) in vec3 aNormal;\r\nlayout (location = 2) in vec2 aTexCoords;\r\n\r\nuniform mat4 model;\r\nuniform mat4 view;\r\nuniform mat4 projection;\r\n\r\nout vec3 Normal;\r\nout vec3 FragPos;\r\n\r\nvoid main()\r\n{\r\n    gl_Position = vec4(aPos, 1.0) * model * view * projection;\r\n    FragPos = vec3(vec4(aPos, 1.0) * model);\r\n    Normal = aNormal * mat3(transpose(inverse(model)));\r\n}\r\n\r\n--Frag\r\n\r\n#version 330 core\r\n\r\nout vec4 FragColor;\r\n\r\n\r\nuniform vec4 color = vec4(1,1,1,1);  \r\n\r\n\r\nvoid main()\r\n{\r\n    FragColor = color;\r\n}";

    private static readonly Dictionary<Type, IResource> _fallbacks = new Dictionary<Type, IResource>();

    public static bool IsInitialized { get; private set; }

    public static void Initialize()
    {
        if (IsInitialized) return;

        Register("error_pink", Texture2D.CreateTexture(1, 1, Color4.Pink, FilterMode.Nearest));
        Register("error_shader", new Shader(ShaderCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)));
        Register("Resources/Audio/error.ogg", new AudioClip("Resources/Audio/error.ogg"));
        Register("error_mesh", GenMesh.CreateCube());

        IsInitialized = true;
    }

    public static void Register<T>(string name, T resource) where T : IResource
    {
        Resources.AddResource(name, resource, true);
        Resources.RemoveResourceFromControll(resource);
        _fallbacks[typeof(T)] = resource;
    }

    public static T Get<T>() where T : IResource
    {
        if (_fallbacks.TryGetValue(typeof(T), out var fallback)) return (T)fallback;

        throw new InvalidOperationException($"[ResourceFallbacks] No fallback for {typeof(T).Name}. Call ResourceFallbacks.Initialize() after the GL context is created.");
    }
}
