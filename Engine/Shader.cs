﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class Shader : IResource
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

        private int _previousHandle;
        private Dictionary<string, int> _previousUniformLocations;

        private HotReloader _hotReloader;

        public Shader()
        {

        }
        public Shader(string shaderPath)
        {
            _shaderPath = shaderPath;
           
            Load();
            _hotReloader = new HotReloader(_shaderPath, OnShaderFileChanged);
        }

        private void OnShaderFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_isProcessingChange)
                return;
            _isProcessingChange = true;
            _hotReloader.Disable(); 

            Task.Run(async () =>
            {
                await Task.Delay(100);
                while (true)
                {
                    try
                    {
                        using (var stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            break;
                        }
                    }
                    catch (IOException) { await Task.Delay(100); }
                }
                EngineWindow._mainThreadActions.Enqueue(() =>
                {
                    ReloadShader();
                    _isProcessingChange = false;
                    _hotReloader.Enable(); 
                });
            });
        }

        private void Load()
        {
            if (!File.Exists(_shaderPath + ShaderFormat))
                throw new ArgumentException("Invalid shader path: " + _shaderPath);

            var (vertexSrc, fragmentSrc, geometrySrc) = ShaderParser.ParseShaders(_shaderPath);

            int vertexShader = CompileShader(ShaderType.VertexShader, vertexSrc);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSrc);
            int geometryShader = 0;
            if (!string.IsNullOrWhiteSpace(geometrySrc))
                geometryShader = CompileShader(ShaderType.GeometryShader, geometrySrc);

            int newHandle = GL.CreateProgram();
            GL.AttachShader(newHandle, vertexShader);
            if (geometryShader != 0)
                GL.AttachShader(newHandle, geometryShader);
            GL.AttachShader(newHandle, fragmentShader);
            LinkProgram(newHandle);

            GL.DetachShader(newHandle, vertexShader);
            GL.DetachShader(newHandle, fragmentShader);
            if (geometryShader != 0)
                GL.DetachShader(newHandle, geometryShader);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            if (geometryShader != 0)
                GL.DeleteShader(geometryShader);

            var newUniforms = CacheUniformLocations(newHandle);
            if (Handle != 0)
            {
                GL.DeleteProgram(Handle);
            }
            _previousHandle = Handle;
            _previousUniformLocations = _uniformLocations;
            Handle = newHandle;
            _uniformLocations = newUniforms;
            Debug.Log($"[Shader] Compiled! Handle:{Handle} Name: {Path.GetFileName(_shaderPath)}", Color4.BlueViolet);
        }

        public void ReloadShader()
        {
            Debug.Log("Reloading shader...");
            _isReloadingShader = true;
            try { Load(); Debug.Log("Shader reloaded successfully."); }
            catch (Exception ex)
            {
                Debug.Error("Shader reload failed: " + ex.Message);
                Debug.Log("Using previous working shader.");
                if (Handle != _previousHandle)
                {
                    if (Handle != 0) GL.DeleteProgram(Handle);
                    Handle = _previousHandle;
                    _uniformLocations = _previousUniformLocations;
                }
            }
            finally { _isReloadingShader = false; }
        }

        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            if (shader == 0)
                throw new Exception("Shader creation failed. Could not create shader handle.");
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int code);
            if (code != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error compiling shader ({type}): {infoLog}");
            }
            return shader;
        }

        private void LinkProgram(int program)
        {
            if (program == 0)
                throw new Exception("Program linking failed. Program handle is 0.");
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int code);
            if (code != (int)All.True)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Error linking program ({program}): {infoLog}");
            }
        }

        private Dictionary<string, int> CacheUniformLocations(int programHandle)
        {
            GL.GetProgram(programHandle, GetProgramParameterName.ActiveUniforms, out int uniformCount);
            var locations = new Dictionary<string, int>();
            for (int i = 0; i < uniformCount; i++)
            {
                string key = GL.GetActiveUniform(programHandle, i, out _, out _);
                int location = GL.GetUniformLocation(programHandle, key);
                locations.Add(key, location);
            }
            return locations;
        }

        public void Use()
        {
            if (_isReloadingShader || Handle == 0) return;
            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName) => GL.GetAttribLocation(Handle, attribName);

        public void SetBool(string name, bool data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data ? 1 : 0);
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
        public void SetMatrix4(string name, Matrix4 data, bool transpose = true)
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
        public void SetVector4(string name, Vector4 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            GL.UseProgram(Handle);
            GL.Uniform4(_uniformLocations[name], data);
        }
        public void SetVector4(string name, Color4 data)
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
            _hotReloader?.Dispose();
            _hotReloader = null;
        }

        public IResource Load(string path)
        {
          
            return new Shader(path);
        }

    }

    public static class ShaderParser
    {
        public static (string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource) ParseShaders(string filePath)
        {
            var lines = File.ReadAllLines(filePath + ".glsl");
            bool inVertex = false, inFragment = false, inGeometry = false;
            string vertexSrc = "", fragmentSrc = "", geometrySrc = "";
            foreach (var line in lines)
            {
                if (line.Contains("--Vert"))
                {
                    inVertex = true; inFragment = false; inGeometry = false;
                    continue;
                }
                if (line.Contains("--Frag"))
                {
                    inVertex = false; inFragment = true; inGeometry = false;
                    continue;
                }
                if (line.Contains("--Geom"))
                {
                    inVertex = false; inFragment = false; inGeometry = true;
                    continue;
                }
                if (inVertex) vertexSrc += line + "\n";
                else if (inFragment) fragmentSrc += line + "\n";
                else if (inGeometry) geometrySrc += line + "\n";
            }
            if (string.IsNullOrWhiteSpace(vertexSrc) || string.IsNullOrWhiteSpace(fragmentSrc))
                throw new Exception("Both vertex and fragment shader sources must be provided.");
            return (vertexSrc, fragmentSrc, geometrySrc);
        }
    }

    public class HotReloader : IDisposable
    {
        private FileSystemWatcher _watcher;
        public HotReloader(string shaderPath, FileSystemEventHandler onChanged)
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(shaderPath), Path.GetFileName(shaderPath) + ".glsl")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            _watcher.Changed += onChanged;
        }
        public void Disable()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = false;
        }
        public void Enable()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = true;
        }
        public void Dispose() => _watcher?.Dispose();
    }

}
