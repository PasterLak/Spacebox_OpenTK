using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Spacebox.Common
{
    public class Shader : IDisposable
    {
        public int Handle;

        private Dictionary<string, int> _uniformLocations;
        private const string ShaderFormat = ".glsl";
        private const string VertexShaderKey = "--Vert";
        private const string FragmentShaderKey = "--Frag";

        private FileSystemWatcher _watcher;
        private string _shaderPath;

        private bool _isProcessingChange = false;
        private readonly SynchronizationContext _syncContext;

        public Shader(string shaderPath)
        {
            _shaderPath = shaderPath;
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            Load();
            InitializeHotReload();
        }

        private void InitializeHotReload()
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(_shaderPath), Path.GetFileName(_shaderPath) + ShaderFormat)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
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
                Thread.Sleep(500);

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
                        Thread.Sleep(500);
                    }
                }

                _syncContext.Post(_ =>
                {
                    ReloadShader();
                    _isProcessingChange = false;
                    _watcher.EnableRaisingEvents = true;
                }, null);
            });
        }

        private void Load()
        {
            if (!File.Exists(_shaderPath + ShaderFormat))
                throw new ArgumentException("Invalid shader path: " + _shaderPath);

            var shaderSources = ParseShaders(_shaderPath);

            var vertexShader = CompileShader(ShaderType.VertexShader, shaderSources.vertexShaderSource);
            var fragmentShader = CompileShader(ShaderType.FragmentShader, shaderSources.fragmentShaderSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            LinkProgram(Handle);

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            CacheUniformLocations();
            Console.WriteLine($"[Shader] Compiled! ID:{Handle} Name: {Path.GetFileName(_shaderPath)}");
        }

        public void ReloadShader()
        {
            GL.UseProgram(0);

            if (Handle != 0)
            {
                Console.WriteLine("Deleting old shader program.");
                GL.DeleteProgram(Handle);
            }

            _uniformLocations = null;
            Load();
        }

        public static (string vertexShaderSource, string fragmentShaderSource) ParseShaders(string filePath)
        {
            var lines = File.ReadAllLines(filePath + ShaderFormat);

            bool inVertexShader = false;
            bool inFragmentShader = false;

            string vertexShaderSource = "";
            string fragmentShaderSource = "";

            foreach (var line in lines)
            {
                if (line.Contains(VertexShaderKey))
                {
                    inVertexShader = true;
                    inFragmentShader = false;
                    continue;
                }

                if (line.Contains(FragmentShaderKey))
                {
                    inVertexShader = false;
                    inFragmentShader = true;
                    continue;
                }

                if (inVertexShader)
                    vertexShaderSource += line + "\n";
                else if (inFragmentShader)
                    fragmentShaderSource += line + "\n";
            }

            if (string.IsNullOrWhiteSpace(vertexShaderSource) || string.IsNullOrWhiteSpace(fragmentShaderSource))
                throw new Exception("Failed to parse shaders: Both vertex and fragment shaders must be present in the file.");

            return (vertexShaderSource, fragmentShaderSource);
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
                throw new Exception($"Error compiling shader ({shader}): {infoLog}");
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

        private void CacheUniformLocations()
        {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            _uniformLocations = new Dictionary<string, int>();

            for (var i = 0; i < numberOfUniforms; i++)
            {
                var key = GL.GetActiveUniform(Handle, i, out _, out _);
                var location = GL.GetUniformLocation(Handle, key);
                _uniformLocations.Add(key, location);
            }
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public void SetInt(string name, int data)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            GL.UseProgram(Handle);
            GL.UniformMatrix4(_uniformLocations[name], true, ref data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], data);
        }

        public void Dispose()
        {
            if (Handle != 0)
                GL.DeleteProgram(Handle);

            _watcher?.Dispose();
        }
    }
}
