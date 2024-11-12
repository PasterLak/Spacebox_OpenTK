using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Drawing;

namespace Spacebox.Common
{
    public class Shader : IDisposable
    {
        public int Handle { get; private set; }

        private Dictionary<string, int> _uniformLocations;
        private const string ShaderFormat = ".glsl";
        private const string VertexShaderKey = "--Vert";
        private const string FragmentShaderKey = "--Frag";
        private const string GeometryShaderKey = "--Geom";

        private FileSystemWatcher _watcher;
        private readonly string _shaderPath;

        private bool _isProcessingChange = false;
        private bool _isReloadingShader = false;

        private readonly ConcurrentQueue<Action> _mainThreadActions;

        // Store previous working shader and uniform locations
        private int _previousHandle;
        private Dictionary<string, int> _previousUniformLocations;

        public Shader(string shaderPath)
        {
            _shaderPath = shaderPath;
            _mainThreadActions = Window._mainThreadActions;
            Load();
            InitializeHotReload();
        }

        private void InitializeHotReload()
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(_shaderPath), Path.GetFileName(_shaderPath) + ShaderFormat)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnShaderFileChanged;
        }

        private void OnShaderFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_isProcessingChange) return;

            _isProcessingChange = true;
            _watcher.EnableRaisingEvents = false;

            Task.Run(() =>
            {
                // Delay to ensure file is ready
                Task.Delay(100).Wait();

                // Wait until the file is accessible
                while (true)
                {
                    try
                    {
                        using (var stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        Task.Delay(500).Wait();
                    }
                }

                // Enqueue the ReloadShader action to be executed on the main thread
                _mainThreadActions.Enqueue(() =>
                {
                    ReloadShader();
                    _isProcessingChange = false;
                    _watcher.EnableRaisingEvents = true;
                });
            });
        }

        private void Load()
        {
            if (!File.Exists(_shaderPath + ShaderFormat))
                throw new ArgumentException("Invalid shader path: " + _shaderPath);

            var shaderSources = ParseShaders(_shaderPath);

            // Compile shaders
            int vertexShader = 0;
            int fragmentShader = 0;
            int geometryShader = 0;

            try
            {
                vertexShader = CompileShader(ShaderType.VertexShader, shaderSources.vertexShaderSource);
                fragmentShader = CompileShader(ShaderType.FragmentShader, shaderSources.fragmentShaderSource);

                if (!string.IsNullOrWhiteSpace(shaderSources.geometryShaderSource))
                {
                    geometryShader = CompileShader(ShaderType.GeometryShader, shaderSources.geometryShaderSource);
                }
            }
            catch (Exception ex)
            {
                // Cleanup shaders if compilation failed
                if (vertexShader != 0) GL.DeleteShader(vertexShader);
                if (fragmentShader != 0) GL.DeleteShader(fragmentShader);
                if (geometryShader != 0) GL.DeleteShader(geometryShader);

                throw new Exception($"Shader <{_shaderPath}> compilation failed: " + ex.Message);
            }

            // Create and link program
            int newHandle = GL.CreateProgram();
            GL.AttachShader(newHandle, vertexShader);
            if (geometryShader != 0)
                GL.AttachShader(newHandle, geometryShader);
            GL.AttachShader(newHandle, fragmentShader);

            try
            {
                LinkProgram(newHandle);
            }
            catch (Exception ex)
            {
                // Cleanup shaders and program if linking failed
                GL.DetachShader(newHandle, vertexShader);
                GL.DetachShader(newHandle, fragmentShader);
                if (geometryShader != 0)
                    GL.DetachShader(newHandle, geometryShader);

                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);
                if (geometryShader != 0)
                    GL.DeleteShader(geometryShader);
                GL.DeleteProgram(newHandle);

                throw new Exception("Shader linking failed: " + ex.Message);
            }

            // Detach and delete shaders after successful linking
            GL.DetachShader(newHandle, vertexShader);
            GL.DetachShader(newHandle, fragmentShader);
            if (geometryShader != 0)
                GL.DetachShader(newHandle, geometryShader);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            if (geometryShader != 0)
                GL.DeleteShader(geometryShader);

            // Cache uniform locations
            var newUniformLocations = CacheUniformLocations(newHandle);

            // If we reach here, everything succeeded
            // Store previous shader program and uniforms
            if (Handle != 0)
            {
                // Delete previous shader program after successful compilation
                GL.DeleteProgram(Handle);
            }

            _previousHandle = Handle;
            _previousUniformLocations = _uniformLocations;

            Handle = newHandle;
            _uniformLocations = newUniformLocations;

            Debug.Log($"[Shader] Compiled! Handle:{Handle} Name: {Path.GetFileName(_shaderPath)}", Color4.BlueViolet);
        }

        public void ReloadShader()
        {
            Debug.Log("Reloading shader...");
            _isReloadingShader = true;

            try
            {
                Load();
                Debug.Log("Shader reloaded successfully.");
            }
            catch (Exception ex)
            {
                // If reloading failed, keep using the previous shader
                Debug.Error("Shader reload failed: " + ex.Message);
                Debug.Log("Using previous working shader.");

                // Restore previous shader and uniforms
                if (Handle != _previousHandle)
                {
                    if (Handle != 0)
                        GL.DeleteProgram(Handle);

                    Handle = _previousHandle;
                    _uniformLocations = _previousUniformLocations;
                }
            }
            finally
            {
                _isReloadingShader = false;
            }
        }

        public static (string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource) ParseShaders(string filePath)
        {
            var lines = File.ReadAllLines(filePath + ShaderFormat);

            bool inVertexShader = false;
            bool inFragmentShader = false;
            bool inGeometryShader = false;

            string vertexShaderSource = "";
            string fragmentShaderSource = "";
            string geometryShaderSource = "";

            foreach (var line in lines)
            {
                if (line.Contains(VertexShaderKey))
                {
                    inVertexShader = true;
                    inFragmentShader = false;
                    inGeometryShader = false;
                    continue;
                }

                if (line.Contains(FragmentShaderKey))
                {
                    inVertexShader = false;
                    inFragmentShader = true;
                    inGeometryShader = false;
                    continue;
                }

                if (line.Contains(GeometryShaderKey))
                {
                    inVertexShader = false;
                    inFragmentShader = false;
                    inGeometryShader = true;
                    continue;
                }

                if (inVertexShader)
                    vertexShaderSource += line + "\n";
                else if (inFragmentShader)
                    fragmentShaderSource += line + "\n";
                else if (inGeometryShader)
                    geometryShaderSource += line + "\n";
            }

            if (string.IsNullOrWhiteSpace(vertexShaderSource) || string.IsNullOrWhiteSpace(fragmentShaderSource))
                throw new Exception("Failed to parse shaders: Both vertex and fragment shaders must be present in the file.");

            return (vertexShaderSource, fragmentShaderSource, geometryShaderSource);
        }

        private int CompileShader(ShaderType type, string source)
        {
            var shader = GL.CreateShader(type);

            if (shader == 0)
                throw new Exception("Shader creation failed. Could not create shader handle.");

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error compiling shader ({type}): {infoLog}");
            }

            return shader;
        }

        private void LinkProgram(int program)
        {
            if (program == 0)
                throw new Exception("Program linking failed. Program handle is 0.");

            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Error linking program ({program}): {infoLog}");
            }
        }

        private Dictionary<string, int> CacheUniformLocations(int programHandle)
        {
            GL.GetProgram(programHandle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            var uniformLocations = new Dictionary<string, int>();

            for (var i = 0; i < numberOfUniforms; i++)
            {
                var key = GL.GetActiveUniform(programHandle, i, out _, out _);
                var location = GL.GetUniformLocation(programHandle, key);
                uniformLocations.Add(key, location);
            }

            return uniformLocations;
        }

        public void Use()
        {
            if (_isReloadingShader || Handle == 0)
                return;

            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public void SetBool(string name, bool data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            byte param = data ? (byte)1 : (byte)0; 

            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], param);
        }

        public void SetInt(string name, int data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.UniformMatrix4(_uniformLocations[name], true, ref data);
        }

        public void SetMatrix4(string name, Matrix4 data, bool transpose)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.UniformMatrix4(_uniformLocations[name], transpose, ref data);
        }

        public void SetVector2(string name, Vector2 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.Uniform2(_uniformLocations[name], data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], data);
        }

        public void SetVector3(string name, Color data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], new Vector3(data.R, data.G, data.B));
        }

        public void SetVector4(string name, Vector4 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;

            GL.UseProgram(Handle);
            GL.Uniform4(_uniformLocations[name], data);
        }

        public void Dispose()
        {
            if (Handle != 0)
                GL.DeleteProgram(Handle);

            _watcher?.Dispose();
        }
    }
}
