using Engine.Grapchics;
using Engine.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine
{
    public class Shader : IResource
    {
        private static int ACTIVE_SHADER = 0;

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

        private void BindUniformBlocks(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.ActiveUniformBlocks, out int count);

            for (int i = 0; i < count; i++)
            {
                GL.GetActiveUniformBlockName(program, i, 64, out int len, out string raw);
                string name = len > 0 ? raw.Substring(0, len) : raw;

                int binding = BindingAllocator.GetOrAssign(name);
                GL.UniformBlockBinding(program, i, binding);
            }
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
            BindUniformBlocks(newHandle);

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
            GPUDebug.LabelProgram(Handle, _shaderPath);
            Debug.Log($"[Shader][{Handle}] Compiled! - {Path.GetFileName(_shaderPath)}", Color4.BlueViolet);
        }

        public void ReloadShader()
        {
            Debug.Log("[Shader] Reloading shader...");
            _isReloadingShader = true;
            ACTIVE_SHADER = 0;
            try { Load(); Debug.Log("[Shader] Shader reloaded successfully."); }
            catch (Exception ex)
            {
                Debug.Error("[Shader] Shader reload failed: " + ex.Message);
                Debug.Log("[Shader] Using previous working shader.");
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
                throw new Exception($"[Shader] Error compiling shader ({type}): {infoLog}");
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
                throw new Exception($"[Shader] Error linking program ({program}): {infoLog}");
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

            if(ACTIVE_SHADER != Handle)
            {
                GL.UseProgram(Handle);
                ACTIVE_SHADER = Handle;
            }

        }

        public int GetAttribLocation(string attribName) => GL.GetAttribLocation(Handle, attribName);

        public void SetBool(string name, bool data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform1(_uniformLocations[name], data ? 1 : 0);
        }
        public void SetInt(string name, int data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform1(_uniformLocations[name], data);
        }
        public void SetFloat(string name, float data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform1(_uniformLocations[name], data);
        }
        public void SetMatrix4(string name, Matrix4 data, bool transpose = true)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.UniformMatrix4(_uniformLocations[name], transpose, ref data);
        }
        public void SetVector2(string name, Vector2 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform2(_uniformLocations[name], data);
        }
        public void SetVector3(string name, Vector3 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform3(_uniformLocations[name], data);
        }
        public void SetVector4(string name, Vector4 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform4(_uniformLocations[name], data);
        }
        public void SetVector4(string name, Color4 data)
        {
            if (_isReloadingShader || Handle == 0 || !_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform4(_uniformLocations[name], data);
        }
        public void Dispose()
        {
            if (Handle != 0)
                GL.DeleteProgram(Handle);

            if(ACTIVE_SHADER == Handle)
            {
                GL.UseProgram(0);
                ACTIVE_SHADER = 0;
            }
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

        public static string RemoveComments(string src)
        {
            // 1) remove /* ... */ 
            string noBlock = Regex.Replace(
                src,
                @"/\*.*?\*/",
                "",
                RegexOptions.Singleline
            );
            // 2) remove // 
            string noLine = Regex.Replace(
                noBlock,
                @"//.*?$",
                "",
                RegexOptions.Multiline
            );
            return noLine;
        }

        public static (string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource)
                ParseShaders(string filePath)
        {

            var lines = File.ReadAllLines(filePath + ".glsl");


            bool inVert = false, inFrag = false, inGeom = false;
            var v = new StringBuilder();
            var f = new StringBuilder();
            var g = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Contains("--Vert")) { inVert = true; inFrag = inGeom = false; continue; }
                if (line.Contains("--Frag")) { inFrag = true; inVert = inGeom = false; continue; }
                if (line.Contains("--Geom")) { inGeom = true; inVert = inFrag = false; continue; }

                if (inVert) v.AppendLine(line);
                if (inFrag) f.AppendLine(line);
                if (inGeom) g.AppendLine(line);
            }

            string dir = Path.GetDirectoryName(filePath)!;

            string vertSrc = ExpandIncludes(v.ToString(), dir);
            string fragSrc = ExpandIncludes(f.ToString(), dir);
            string geomSrc = ExpandIncludes(g.ToString(), dir);


            vertSrc = RemoveComments(vertSrc);
            fragSrc = RemoveComments(fragSrc);
            geomSrc = RemoveComments(geomSrc);

            if (string.IsNullOrWhiteSpace(vertSrc) || string.IsNullOrWhiteSpace(fragSrc))
                throw new Exception("Both vertex and fragment shader sources must be provided.");

            return (vertSrc, fragSrc, geomSrc);
        }

        static string ExpandIncludes(string src, string dir, HashSet<string>? visited = null)
        {
            visited ??= new HashSet<string>();
            var sb = new StringBuilder();
            int lineNo = 1;
            bool globalsInjected = false;

            foreach (var raw in src.Split('\n'))
            {
                var t = raw.TrimStart();


                if (t.StartsWith("#include"))
                {
                    int q1 = t.IndexOf('"') + 1;
                    int q2 = t.LastIndexOf('"');
                    if (q1 <= 0 || q2 <= q1) continue;

                    string incName = t[q1..q2];
                    string incPath = Path.Combine(dir, incName);

                    if (!visited.Add(incPath)) continue;
                    if (!File.Exists(incPath))
                        throw new FileNotFoundException($"Include not found: {incPath}");

                    string included = File.ReadAllText(incPath);
                    sb.AppendLine(ExpandIncludes(included, Path.GetDirectoryName(incPath)!, visited));
                    sb.AppendLine($"#line {++lineNo}");
                }
                else if (t.StartsWith("#version"))
                {

                    if (!globalsInjected && t.StartsWith("#version"))
                    {
                        sb.AppendLine(raw);
                        sb.AppendLine(GlobalUniforms.GenerateCode());
                        globalsInjected = true;
                    }
                    lineNo++;

                }

                else
                {
                    sb.AppendLine(raw);
                    lineNo++;
                }
            }
            return sb.ToString();
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
